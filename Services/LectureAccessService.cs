using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Models;
using ElectronicStudyCabinet.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStudyCabinet.Services;

public enum LectureAccessStatus
{
    Allowed,   // доступ разрешён
    NotFound,  // лекции не существует
    Denied     // доступ запрещён
}

public record LectureAccessResult(
    LectureAccessStatus Status,
    string? Message = null,
    LectureStateViewModel? State = null)
{
    public bool IsAllowed => Status == LectureAccessStatus.Allowed;
}

// Единое место, где решается, может ли студент открыть лекцию
public interface ILectureAccessService
{
    Task<LectureAccessResult> CheckAsync(string studentId, int lectureId);
    Task<List<LectureStateViewModel>> GetDisciplineStatesAsync(string studentId, int disciplineId);
}

public class LectureAccessService : ILectureAccessService
{
    private const string DeniedMessage = "Доступ к лекции запрещён";

    private readonly ApplicationDbContext _db;

    public LectureAccessService(ApplicationDbContext db)
    {
        _db = db;
    }

    // Можно ли студенту открыть эту лекцию
    public async Task<LectureAccessResult> CheckAsync(string studentId, int lectureId)
    {
        var info = await _db.Lectures
            .AsNoTracking()
            .Where(l => l.Id == lectureId)
            .Select(l => new
            {
                l.IsPublished,
                l.Topic.DisciplineId,
                DisciplinePublished = l.Topic.Discipline.IsPublished
            })
            .FirstOrDefaultAsync();

        if (info == null)
            return new(LectureAccessStatus.NotFound);

        if (!info.IsPublished || !info.DisciplinePublished)
            return new(LectureAccessStatus.Denied, DeniedMessage);

        var states = await GetDisciplineStatesAsync(studentId, info.DisciplineId);
        var state = states.FirstOrDefault(s => s.LectureId == lectureId);

        if (state == null || state.IsLocked)
            return new(LectureAccessStatus.Denied, DeniedMessage);

        return new(LectureAccessStatus.Allowed, null, state);
    }

    // Статусы всех лекций дисциплины для студента (ничего не записывает в базу)
    public async Task<List<LectureStateViewModel>> GetDisciplineStatesAsync(string studentId, int disciplineId)
    {
        var disciplinePublished = await _db.Disciplines
            .AnyAsync(d => d.Id == disciplineId && d.IsPublished);

        if (!disciplinePublished)
            return new();

        // 1. Опубликованные лекции в сквозном порядке: тема, затем лекция в теме
        var lectures = await _db.Lectures
            .AsNoTracking()
            .Where(l => l.Topic.DisciplineId == disciplineId && l.IsPublished)
            .OrderBy(l => l.Topic.OrderIndex).ThenBy(l => l.TopicId)
            .ThenBy(l => l.OrderIndex).ThenBy(l => l.Id)
            .Select(l => new
            {
                l.Id,
                l.TopicId,
                TopicName = l.Topic.Name,
                l.Title,
                l.Description,
                l.MaterialFileName,
                HasMaterial = l.MaterialPath != null,
                l.PassingPercentage,
                l.MaxAttempts,
                l.RequireControlQuestions,
                // Тест учитывается, только если он опубликован и в нём есть вопросы
                TestId = l.Test != null && l.Test.IsPublished && l.Test.Questions.Any()
                    ? (int?)l.Test.Id
                    : null
            })
            .ToListAsync();

        if (lectures.Count == 0)
            return new();

        var lectureIds = lectures.Select(l => l.Id).ToList();
        var testIds = lectures.Where(l => l.TestId != null).Select(l => l.TestId!.Value).ToList();

        // 2. Какие тесты студент уже прошёл
        var passedTestIds = (await _db.TestAttempts
                .AsNoTracking()
                .Where(a => a.StudentId == studentId && a.IsPassed && a.CompletedAt != null
                            && testIds.Contains(a.TestId))
                .Select(a => a.TestId)
                .Distinct()
                .ToListAsync())
            .ToHashSet();

        // 3. Обязательные контрольные вопросы и ответы студента
        var requiredQuestions = await _db.ControlQuestions
            .AsNoTracking()
            .Where(q => lectureIds.Contains(q.LectureId) && q.IsRequired)
            .Select(q => new { q.Id, q.LectureId, q.RequiresManualReview })
            .ToListAsync();

        var questionIds = requiredQuestions.Select(q => q.Id).ToList();

        var answers = (await _db.StudentAnswers
                .AsNoTracking()
                .Where(a => a.StudentId == studentId && questionIds.Contains(a.ControlQuestionId))
                .Select(a => new { a.ControlQuestionId, a.IsCorrect, a.ReviewStatus })
                .ToListAsync())
            .ToDictionary(a => a.ControlQuestionId);

        // 4. Записи прогресса студента
        var progress = (await _db.LectureProgress
                .AsNoTracking()
                .Where(p => p.StudentId == studentId && lectureIds.Contains(p.LectureId))
                .ToListAsync())
            .ToDictionary(p => p.LectureId);

        // 5. Проходим по лекциям по порядку
        var result = new List<LectureStateViewModel>();
        var previousCompleted = true; // перед первой лекцией "предыдущей" нет, она открыта сразу
        var number = 0;

        foreach (var lecture in lectures)
        {
            number++;
            progress.TryGetValue(lecture.Id, out var record);

            // Лекция "открыта студентом", если у неё есть запись прогресса со статусом изучения
            var opened = record != null &&
                         record.Status is LectureStatus.InProgress
                             or LectureStatus.PendingReview
                             or LectureStatus.Completed;

            var testRequired = lecture.TestId != null;
            var testPassed = !testRequired || passedTestIds.Contains(lecture.TestId!.Value);

            // Обязательные контрольные вопросы учитываются, только если это включено в настройках лекции
            var requiredCount = 0;
            var doneCount = 0;
            var pendingCount = 0;
            var unmetCount = 0;

            if (lecture.RequireControlQuestions)
            {
                foreach (var question in requiredQuestions.Where(q => q.LectureId == lecture.Id))
                {
                    requiredCount++;

                    if (!answers.TryGetValue(question.Id, out var answer))
                    {
                        unmetCount++;                       // ответа ещё нет
                    }
                    else if (question.RequiresManualReview)
                    {
                        if (answer.ReviewStatus == ReviewStatus.Accepted)
                            doneCount++;                    // принят преподавателем
                        else if (answer.ReviewStatus == ReviewStatus.PendingReview)
                            pendingCount++;                 // ждёт проверки
                        else
                            unmetCount++;                   // отклонён
                    }
                    else if (answer.IsCorrect)
                    {
                        doneCount++;                        // автоматическая проверка: верно
                    }
                    else
                    {
                        unmetCount++;                       // автоматическая проверка: неверно
                    }
                }
            }

            var completedNow = opened && testPassed && unmetCount == 0 && pendingCount == 0;
            var storedCompleted = record?.Status == LectureStatus.Completed;
            var manuallyUnlocked = record?.IsManuallyUnlocked ?? false;

            // Лекция открыта: если предыдущая завершена, если преподаватель открыл вручную
            // или если студент уже завершил её раньше
            var unlocked = previousCompleted || manuallyUnlocked || storedCompleted;

            LectureStatus status;
            if (!unlocked)
                status = LectureStatus.Locked;
            else if (storedCompleted || completedNow)
                status = LectureStatus.Completed;
            else if (opened && testPassed && unmetCount == 0 && pendingCount > 0)
                status = LectureStatus.PendingReview;
            else if (opened)
                status = LectureStatus.InProgress;
            else
                status = LectureStatus.Available;

            result.Add(new LectureStateViewModel
            {
                LectureId = lecture.Id,
                TopicId = lecture.TopicId,
                TopicName = lecture.TopicName,
                Number = number,
                Title = lecture.Title,
                Description = lecture.Description,
                Status = status,
                IsManuallyUnlocked = manuallyUnlocked,
                HasMaterial = lecture.HasMaterial,
                MaterialFileName = lecture.MaterialFileName,
                PassingPercentage = lecture.PassingPercentage,
                MaxAttempts = lecture.MaxAttempts,
                TestRequired = testRequired,
                TestPassed = testRequired && testPassed,
                RequiredQuestions = requiredCount,
                RequiredQuestionsDone = doneCount,
                PendingReviewCount = pendingCount
            });

            previousCompleted = status == LectureStatus.Completed;
        }

        return result;
    }
}