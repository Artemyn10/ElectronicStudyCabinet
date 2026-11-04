using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Models;
using ElectronicStudyCabinet.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStudyCabinet.Services;

public interface ITeacherReportService
{
    Task<StudentsListViewModel> GetStudentsAsync(string? search, string? status, int? groupId);
    Task<StudentDetailsViewModel?> GetStudentDetailsAsync(string studentId);
    Task<ResultsPageViewModel> GetResultsAsync(int? disciplineId, string? student, string? status, int page);
    Task<ProgressPageViewModel> GetProgressAsync(int? disciplineId);
    Task<ReportsViewModel> GetReportsAsync();
}

public class TeacherReportService : ITeacherReportService
{
    private const int PageSize = 20;

    private readonly ApplicationDbContext _db;
    private readonly ILectureAccessService _access;
    private readonly IStudentCabinetService _cabinet;

    public TeacherReportService(
        ApplicationDbContext db,
        ILectureAccessService access,
        IStudentCabinetService cabinet)
    {
        _db = db;
        _access = access;
        _cabinet = cabinet;
    }

    // Простые записи для расчётов в памяти
    private record StudentInfo(string Id, string FullName, string? UserName, string? Email);
    private record DisciplineTotals(int Id, string Name, int Total);
    private record ProgressRow(string StudentId, int DisciplineId, LectureStatus Status);
    private record AttemptRow(string StudentId, int DisciplineId, bool Completed, DateTime Time, double Score, bool IsPassed);
    private record GroupInfo(int GroupId, string GroupName, string SpecialtyName);
    private class ReportData
    {
        public List<StudentInfo> Students { get; set; } = new();
        public Dictionary<int, DisciplineTotals> Totals { get; set; } = new();
        public List<ProgressRow> Progress { get; set; } = new();
        public List<AttemptRow> Attempts { get; set; } = new();
    }

    // Студенты = пользователи с ролью Student
    private IQueryable<ApplicationUser> StudentsQuery()
    {
        return _db.Users
            .AsNoTracking()
            .Where(u => _db.UserRoles.Any(ur => ur.UserId == u.Id
                        && _db.Roles.Any(r => r.Id == ur.RoleId && r.Name == AppRoles.Student)));
    }

    // ===== Студенты =====

    public async Task<StudentsListViewModel> GetStudentsAsync(string? search, string? status, int? groupId)
    {
        var data = await LoadDataAsync(search);
        var rows = BuildStudentRows(data);

        // Группа и специальность каждого студента
        var groups = await LoadGroupsAsync(rows.Select(r => r.Id).ToList());

        foreach (var row in rows)
        {
            if (groups.TryGetValue(row.Id, out var group))
            {
                row.GroupName = group.GroupName;
                row.SpecialtyName = group.SpecialtyName;
            }
        }

        // Фильтр по группе: 0 означает "Без группы"
        if (groupId != null)
        {
            rows = rows
                .Where(r => groupId == 0
                    ? !groups.ContainsKey(r.Id)
                    : groups.TryGetValue(r.Id, out var g) && g.GroupId == groupId)
                .ToList();
        }

        rows = status switch
        {
            "notstarted" => rows.Where(r => r.StartedDisciplines == 0).ToList(),
            "inprogress" => rows.Where(r => r.StartedDisciplines > 0 && r.ProgressPercent < 100).ToList(),
            "finished" => rows.Where(r => r.TotalLectures > 0 && r.CompletedLectures >= r.TotalLectures).ToList(),
            _ => rows
        };

        return new StudentsListViewModel
        {
            Search = search,
            Status = status,
            GroupId = groupId,
            Items = rows,
            Groups = await LoadGroupOptionsAsync()
        };
    }

    public async Task<StudentDetailsViewModel?> GetStudentDetailsAsync(string studentId)
    {
        // Убеждаемся, что это именно студент
        var user = await StudentsQuery().FirstOrDefaultAsync(u => u.Id == studentId);
        if (user == null)
            return null;

        var cabinet = await _cabinet.GetCabinetAsync(user.Id, user.FullName);

        // Заблокированные лекции, которые преподаватель может открыть вручную
        var lockedLectures = cabinet.Disciplines
            .SelectMany(d => d.Topics
                .SelectMany(t => t.Lectures)
                .Where(l => l.State.IsLocked)
                .Select(l => new LockedLectureOptionViewModel
                {
                    LectureId = l.State.LectureId,
                    DisciplineName = d.Name,
                    Number = l.State.Number,
                    Title = l.State.Title
                }))
            .ToList();

        var membership = await _db.GroupMemberships
            .AsNoTracking()
            .Where(m => m.StudentId == studentId)
            .Select(m => new
            {
                m.StudentGroupId,
                GroupName = m.StudentGroup.Name,
                SpecialtyName = m.StudentGroup.Specialty.Name
            })
            .FirstOrDefaultAsync();

        return new StudentDetailsViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            UserName = user.UserName,
            Email = user.Email,
            Cabinet = cabinet,
            LockedLectures = lockedLectures,
            GroupId = membership?.StudentGroupId,
            GroupName = membership?.GroupName,
            SpecialtyName = membership?.SpecialtyName,
            Groups = await LoadGroupOptionsAsync()
        };
    }

    // ===== Результаты =====

    public async Task<ResultsPageViewModel> GetResultsAsync(int? disciplineId, string? student, string? status, int page)
    {
        var query = _db.TestAttempts
            .AsNoTracking()
            .Where(a => a.CompletedAt != null);

        if (disciplineId != null)
            query = query.Where(a => a.Test.Lecture.Topic.DisciplineId == disciplineId);

        if (!string.IsNullOrWhiteSpace(student))
        {
            var pattern = $"%{student.Trim()}%";
            query = query.Where(a => EF.Functions.ILike(a.Student.FullName, pattern)
                                     || EF.Functions.ILike(a.Student.UserName ?? "", pattern));
        }

        if (status == "passed")
            query = query.Where(a => a.IsPassed);
        else if (status == "failed")
            query = query.Where(a => !a.IsPassed);

        var totalCount = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var items = await query
            .OrderByDescending(a => a.CompletedAt).ThenByDescending(a => a.Id)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(a => new ResultRowViewModel
            {
                AttemptId = a.Id,
                StudentId = a.StudentId,
                StudentName = a.Student.FullName,
                DisciplineName = a.Test.Lecture.Topic.Discipline.Name,
                LectureTitle = a.Test.Lecture.Title,
                AttemptNumber = a.AttemptNumber,
                CorrectAnswers = a.CorrectAnswers,
                TotalQuestions = a.TotalQuestions,
                ScorePercentage = a.ScorePercentage,
                PassingPercentage = a.Test.Lecture.PassingPercentage,
                IsPassed = a.IsPassed,
                CompletedAt = a.CompletedAt ?? a.StartedAt
            })
            .ToListAsync();

        var disciplines = await _db.Disciplines
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DisciplineOptionViewModel { Id = d.Id, Name = d.Name })
            .ToListAsync();

        return new ResultsPageViewModel
        {
            DisciplineId = disciplineId,
            Student = student,
            Status = status,
            Page = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            Items = items,
            Disciplines = disciplines
        };
    }

    // ===== Прогресс обучения =====

    public async Task<ProgressPageViewModel> GetProgressAsync(int? disciplineId)
    {
        var disciplines = await _db.Disciplines
            .AsNoTracking()
            .Where(d => d.IsPublished)
            .OrderBy(d => d.Name)
            .Select(d => new DisciplineOptionViewModel { Id = d.Id, Name = d.Name })
            .ToListAsync();

        var model = new ProgressPageViewModel { Disciplines = disciplines };

        if (disciplines.Count == 0)
            return model;

        var selected = disciplines.FirstOrDefault(d => d.Id == disciplineId) ?? disciplines[0];
        model.SelectedDisciplineId = selected.Id;
        model.SelectedName = selected.Name;

        var students = await StudentsQuery()
            .OrderBy(u => u.FullName)
            .Take(100)
            .Select(u => new { u.Id, u.FullName })
            .ToListAsync();

        foreach (var student in students)
        {
            // Статусы считаются тем же кодом, который блокирует лекции у студента
            var states = await _access.GetDisciplineStatesAsync(student.Id, selected.Id);

            if (model.Lectures.Count == 0)
            {
                model.Lectures = states
                    .Select(s => new ProgressLectureHeaderViewModel { Number = s.Number, Title = s.Title })
                    .ToList();
            }

            var completed = states.Count(s => s.Status == LectureStatus.Completed);

            model.Rows.Add(new ProgressStudentRowViewModel
            {
                StudentId = student.Id,
                StudentName = student.FullName,
                Statuses = states.Select(s => s.Status).ToList(),
                Completed = completed,
                Total = states.Count,
                Percent = Percent(completed, states.Count)
            });
        }

        return model;
    }

    // ===== Отчёты =====

    public async Task<ReportsViewModel> GetReportsAsync()
    {
        var data = await LoadDataAsync(null);
        var model = new ReportsViewModel { Students = BuildStudentRows(data) };

        foreach (var discipline in data.Totals.Values.OrderBy(t => t.Name))
        {
            // Сколько лекций завершил каждый студент, начавший эту дисциплину
            var completedByStudent = data.Progress
                .Where(p => p.DisciplineId == discipline.Id)
                .GroupBy(p => p.StudentId)
                .Select(g => g.Count(p => p.Status == LectureStatus.Completed))
                .ToList();

            var attempts = data.Attempts
                .Where(a => a.DisciplineId == discipline.Id && a.Completed)
                .ToList();

            var passed = attempts.Count(a => a.IsPassed);

            model.Disciplines.Add(new DisciplineReportRowViewModel
            {
                Name = discipline.Name,
                LecturesCount = discipline.Total,
                StudentsStarted = completedByStudent.Count,
                StudentsFinished = completedByStudent.Count(c => c >= discipline.Total),
                AverageProgress = completedByStudent.Count == 0
                    ? 0
                    : (int)Math.Round(completedByStudent.Average(c => c * 100.0 / discipline.Total)),
                AttemptsCount = attempts.Count,
                PassedAttempts = passed,
                PassRate = Percent(passed, attempts.Count),
                AverageScore = attempts.Count == 0 ? null : Math.Round(attempts.Average(a => a.Score), 1)
            });
        }

        return model;
    }

    // ===== Вспомогательные методы =====

    // Загружает студентов, число опубликованных лекций, прогресс и попытки
    private async Task<ReportData> LoadDataAsync(string? search)
    {
        var studentsQuery = StudentsQuery();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            studentsQuery = studentsQuery.Where(u =>
                EF.Functions.ILike(u.FullName, pattern)
                || EF.Functions.ILike(u.UserName ?? "", pattern)
                || EF.Functions.ILike(u.Email ?? "", pattern));
        }

        var students = await studentsQuery
            .OrderBy(u => u.FullName)
            .Select(u => new StudentInfo(u.Id, u.FullName, u.UserName, u.Email))
            .ToListAsync();

        var studentIds = students.Select(s => s.Id).ToList();

        // Опубликованные лекции по опубликованным дисциплинам
        var totals = (await _db.Lectures
                .AsNoTracking()
                .Where(l => l.IsPublished && l.Topic.Discipline.IsPublished)
                .GroupBy(l => new { l.Topic.DisciplineId, l.Topic.Discipline.Name })
                .Select(g => new DisciplineTotals(g.Key.DisciplineId, g.Key.Name, g.Count()))
                .ToListAsync())
            .ToDictionary(t => t.Id);

        // Записи прогресса студентов, начавших обучение
        var progress = await _db.LectureProgress
            .AsNoTracking()
            .Where(p => studentIds.Contains(p.StudentId)
                        && p.Status != LectureStatus.NotStudied
                        && p.Lecture.IsPublished
                        && p.Lecture.Topic.Discipline.IsPublished)
            .Select(p => new ProgressRow(p.StudentId, p.Lecture.Topic.DisciplineId, p.Status))
            .ToListAsync();

        var attempts = await _db.TestAttempts
            .AsNoTracking()
            .Where(a => studentIds.Contains(a.StudentId))
            .Select(a => new AttemptRow(
                a.StudentId,
                a.Test.Lecture.Topic.DisciplineId,
                a.CompletedAt != null,
                a.CompletedAt ?? a.StartedAt,
                a.ScorePercentage,
                a.IsPassed))
            .ToListAsync();

        return new ReportData
        {
            Students = students,
            Totals = totals,
            Progress = progress,
            Attempts = attempts
        };
    }

    // Одна строка на студента: прогресс и результаты тестов
    private static List<StudentRowViewModel> BuildStudentRows(ReportData data)
    {
        var totalLectures = data.Totals.Values.Sum(t => t.Total);
        var progressByStudent = data.Progress.ToLookup(p => p.StudentId);
        var attemptsByStudent = data.Attempts.ToLookup(a => a.StudentId);

        return data.Students.Select(student =>
        {
            var progress = progressByStudent[student.Id].ToList();
            var attempts = attemptsByStudent[student.Id].ToList();
            var completedAttempts = attempts.Where(a => a.Completed).ToList();
            var completed = progress.Count(p => p.Status == LectureStatus.Completed);

            return new StudentRowViewModel
            {
                Id = student.Id,
                FullName = student.FullName,
                UserName = student.UserName,
                Email = student.Email,
                StartedDisciplines = progress.Select(p => p.DisciplineId).Distinct().Count(),
                CompletedLectures = completed,
                TotalLectures = totalLectures,
                ProgressPercent = Percent(completed, totalLectures),
                AttemptsCount = completedAttempts.Count,
                PassedAttempts = completedAttempts.Count(a => a.IsPassed),
                AverageScore = completedAttempts.Count == 0
                    ? null
                    : Math.Round(completedAttempts.Average(a => a.Score), 1),
                LastActivity = attempts.Count == 0 ? null : attempts.Max(a => a.Time)
            };
        }).ToList();
    }

    private static int Percent(int part, int total)
    {
        return total == 0 ? 0 : (int)Math.Round(part * 100.0 / total);
    }
    // Группа и специальность для списка студентов: Id студента -> данные группы
    private async Task<Dictionary<string, GroupInfo>> LoadGroupsAsync(List<string> studentIds)
    {
        return (await _db.GroupMemberships
                .AsNoTracking()
                .Where(m => studentIds.Contains(m.StudentId))
                .Select(m => new
                {
                    m.StudentId,
                    m.StudentGroupId,
                    GroupName = m.StudentGroup.Name,
                    SpecialtyName = m.StudentGroup.Specialty.Name
                })
                .ToListAsync())
            .ToDictionary(x => x.StudentId, x => new GroupInfo(x.StudentGroupId, x.GroupName, x.SpecialtyName));
    }

    // Список групп для выпадающих полей: "ИС-21 — Информационные системы..."
    private async Task<List<SimpleOptionViewModel>> LoadGroupOptionsAsync()
    {
        return (await _db.StudentGroups
                .AsNoTracking()
                .OrderBy(g => g.Name)
                .Select(g => new { g.Id, g.Name, Specialty = g.Specialty.Name })
                .ToListAsync())
            .Select(g => new SimpleOptionViewModel { Id = g.Id, Name = $"{g.Name} — {g.Specialty}" })
            .ToList();
    }
}