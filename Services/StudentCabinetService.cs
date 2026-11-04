using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Models;
using ElectronicStudyCabinet.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStudyCabinet.Services;

public interface IStudentCabinetService
{
    Task<StudentCabinetViewModel> GetCabinetAsync(string studentId, string fullName);
}

public class StudentCabinetService : IStudentCabinetService
{
    private readonly ApplicationDbContext _db;
    private readonly ILectureAccessService _access;

    public StudentCabinetService(ApplicationDbContext db, ILectureAccessService access)
    {
        _db = db;
        _access = access;
    }

    public async Task<StudentCabinetViewModel> GetCabinetAsync(string studentId, string fullName)
    {
        // Опубликованные дисциплины
        var disciplines = await _db.Disciplines
            .AsNoTracking()
            .Where(d => d.IsPublished)
            .OrderBy(d => d.Name)
            .Select(d => new { d.Id, d.Name, d.Description })
            .ToListAsync();

        // Все попытки этого студента (их немного), группируем по лекциям
        var attempts = await _db.TestAttempts
            .AsNoTracking()
            .Where(a => a.StudentId == studentId)
            .Select(a => new
            {
                LectureId = a.Test.LectureId,
                a.CompletedAt,
                a.ScorePercentage
            })
            .ToListAsync();

        var attemptsByLecture = attempts
            .GroupBy(a => a.LectureId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Статистика по тесту лекции: сколько попыток, лучший результат, дата последней
        (int Used, double? Best, DateTime? Last) GetStats(int lectureId)
        {
            if (!attemptsByLecture.TryGetValue(lectureId, out var list))
                return (0, null, null);

            var completed = list.Where(a => a.CompletedAt != null).ToList();
            if (completed.Count == 0)
                return (list.Count, null, null);

            return (list.Count, (double?)completed.Max(a => a.ScorePercentage), completed.Max(a => a.CompletedAt));
        }

        var model = new StudentCabinetViewModel { FullName = fullName };
        // Группа и специальность студента (назначает преподаватель)
        var membership = await _db.GroupMemberships
            .AsNoTracking()
            .Where(m => m.StudentId == studentId)
            .Select(m => new { m.StudentGroup.Name, SpecialtyName = m.StudentGroup.Specialty.Name })
            .FirstOrDefaultAsync();

        if (membership != null)
        {
            model.GroupName = membership.Name;
            model.SpecialtyName = membership.SpecialtyName;
        }
        var results = new List<CabinetResultRowViewModel>();

        foreach (var discipline in disciplines)
        {
            // Те же статусы, по которым сервер блокирует лекции
            var states = await _access.GetDisciplineStatesAsync(studentId, discipline.Id);
            var completedCount = states.Count(s => s.Status == LectureStatus.Completed);

            var rows = states.Select(state =>
            {
                var stats = GetStats(state.LectureId);
                return new CabinetLectureViewModel
                {
                    State = state,
                    AttemptsUsed = stats.Used,
                    BestScore = stats.Best,
                    LastAttemptAt = stats.Last
                };
            }).ToList();

            // С какой лекции продолжить: первая открытая и незавершённая
            var next = states.FirstOrDefault(s =>
                s.Status is LectureStatus.Available or LectureStatus.InProgress);

            model.Disciplines.Add(new CabinetDisciplineViewModel
            {
                Id = discipline.Id,
                Name = discipline.Name,
                Description = discipline.Description,
                TotalLectures = states.Count,
                CompletedLectures = completedCount,
                ProgressPercent = Percent(completedCount, states.Count),
                ContinueLectureId = next?.LectureId,
                ContinueLectureNumber = next?.Number ?? 0,
                ContinueLectureTitle = next?.Title,
                Topics = rows
                    .GroupBy(r => r.State.TopicId)
                    .Select(g => new CabinetTopicViewModel
                    {
                        TopicId = g.Key,
                        Name = g.First().State.TopicName,
                        Lectures = g.ToList()
                    })
                    .ToList()
            });

            // Таблица результатов: лекции, по которым были попытки
            results.AddRange(rows
                .Where(r => r.AttemptsUsed > 0)
                .Select(r => new CabinetResultRowViewModel
                {
                    LectureId = r.State.LectureId,
                    LectureTitle = r.State.Title,
                    DisciplineName = discipline.Name,
                    PassingPercentage = r.State.PassingPercentage,
                    MaxAttempts = r.State.MaxAttempts,
                    AttemptsUsed = r.AttemptsUsed,
                    BestScore = r.BestScore,
                    LastAttemptAt = r.LastAttemptAt,
                    IsPassed = r.State.TestPassed
                }));

            model.TotalLectures += states.Count;
            model.CompletedLectures += completedCount;
        }

        model.DisciplinesCount = disciplines.Count;
        model.ProgressPercent = Percent(model.CompletedLectures, model.TotalLectures);
        model.Results = results
            .OrderByDescending(r => r.LastAttemptAt ?? DateTime.MinValue)
            .ToList();

        return model;
    }

    private static int Percent(int part, int total)
    {
        return total == 0 ? 0 : (int)Math.Round(part * 100.0 / total);
    }
}