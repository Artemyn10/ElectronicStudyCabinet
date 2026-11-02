using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Models;
using ElectronicStudyCabinet.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStudyCabinet.Services;

public interface ITeacherDashboardService
{
    Task<TeacherDashboardViewModel> GetAsync();
}

public class TeacherDashboardService : ITeacherDashboardService
{
    private readonly ApplicationDbContext _db;

    public TeacherDashboardService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<TeacherDashboardViewModel> GetAsync()
    {
        var model = new TeacherDashboardViewModel
        {
            DisciplinesCount = await _db.Disciplines.CountAsync(),
            PublishedDisciplinesCount = await _db.Disciplines.CountAsync(d => d.IsPublished),
            LecturesCount = await _db.Lectures.CountAsync(),

            // Попытки, которые студент начал, но ещё не завершил
            UnfinishedAttemptsCount = await _db.TestAttempts.CountAsync(a => a.CompletedAt == null),

            // Ответы, ожидающие ручной проверки
            PendingReviewsCount = await _db.StudentAnswers
                .CountAsync(a => a.ReviewStatus == ReviewStatus.PendingReview)
        };

        // Студенты = пользователи с ролью Student
        var studentRoleId = await _db.Roles
            .Where(r => r.Name == AppRoles.Student)
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        model.StudentsCount = studentRoleId == null
            ? 0
            : await _db.UserRoles.CountAsync(ur => ur.RoleId == studentRoleId);

        // Последние завершённые попытки
        model.LatestResults = await _db.TestAttempts
            .AsNoTracking()
            .Where(a => a.CompletedAt != null)
            .OrderByDescending(a => a.CompletedAt)
            .Take(8)
            .Select(a => new DashboardResultRowViewModel
            {
                StudentName = a.Student.FullName,
                LectureTitle = a.Test.Lecture.Title,
                DisciplineName = a.Test.Lecture.Topic.Discipline.Name,
                CorrectAnswers = a.CorrectAnswers,
                TotalQuestions = a.TotalQuestions,
                ScorePercentage = a.ScorePercentage,
                IsPassed = a.IsPassed,
                CompletedAt = a.CompletedAt ?? a.StartedAt
            })
            .ToListAsync();

        model.UnfinishedStudents = await GetUnfinishedStudentsAsync();

        return model;
    }

    // Студенты, которые начали изучать дисциплину, но не завершили её
    private async Task<List<UnfinishedStudentRowViewModel>> GetUnfinishedStudentsAsync()
    {
        // Сколько опубликованных лекций в каждой опубликованной дисциплине
        var totals = (await _db.Lectures
                .AsNoTracking()
                .Where(l => l.IsPublished && l.Topic.Discipline.IsPublished)
                .GroupBy(l => new { l.Topic.DisciplineId, DisciplineName = l.Topic.Discipline.Name })
                .Select(g => new { g.Key.DisciplineId, g.Key.DisciplineName, Total = g.Count() })
                .ToListAsync())
            .ToDictionary(x => x.DisciplineId);

        // Записи прогресса студентов, которые уже начали обучение
        var progressRows = await _db.LectureProgress
            .AsNoTracking()
            .Where(p => p.Status != LectureStatus.NotStudied
                        && p.Lecture.IsPublished
                        && p.Lecture.Topic.Discipline.IsPublished)
            .Select(p => new
            {
                p.StudentId,
                StudentName = p.Student.FullName,
                DisciplineId = p.Lecture.Topic.DisciplineId,
                p.Status
            })
            .ToListAsync();

        return progressRows
            .GroupBy(p => new { p.StudentId, p.StudentName, p.DisciplineId })
            .Select(g => new
            {
                g.Key.StudentName,
                g.Key.DisciplineId,
                Completed = g.Count(p => p.Status == LectureStatus.Completed)
            })
            .Where(x => totals.ContainsKey(x.DisciplineId) && x.Completed < totals[x.DisciplineId].Total)
            .Select(x => new UnfinishedStudentRowViewModel
            {
                StudentName = x.StudentName,
                DisciplineName = totals[x.DisciplineId].DisciplineName,
                CompletedLectures = x.Completed,
                TotalLectures = totals[x.DisciplineId].Total,
                ProgressPercent = (int)Math.Round(x.Completed * 100.0 / totals[x.DisciplineId].Total)
            })
            .OrderBy(r => r.StudentName)
            .ThenBy(r => r.DisciplineName)
            .Take(10)
            .ToList();
    }
}