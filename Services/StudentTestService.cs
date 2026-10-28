using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Models;
using ElectronicStudyCabinet.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStudyCabinet.Services;

// Что произошло при обработке запроса студента
public enum TestOutcome
{
    Ok,
    NotFound,
    Forbidden,         // чужая попытка
    LectureLocked,     // доступ к лекции запрещён
    NoTest,            // теста нет, он не опубликован или в нём нет вопросов
    AttemptsExhausted, // попытки закончились
    AlreadyPassed,     // тест уже пройден
    AlreadyCompleted   // попытка уже завершена
}

public record TestResponse<T>(TestOutcome Outcome, T? Value = default);

public interface IStudentTestService
{
    Task<List<AvailableTestViewModel>> GetAvailableTestsAsync(string studentId);
    Task<TestResponse<StudentTestOverviewViewModel>> GetOverviewAsync(string studentId, int lectureId);
    Task<TestResponse<int>> StartAsync(string studentId, int lectureId);
    Task<TestResponse<StudentTestTakeViewModel>> GetTakeAsync(string studentId, int attemptId);
    Task<TestResponse<int>> SubmitAsync(string studentId, int attemptId, Dictionary<int, int> answers);
    Task<TestResponse<StudentTestResultViewModel>> GetResultAsync(string studentId, int attemptId);
}

public class StudentTestService : IStudentTestService
{
    private readonly ApplicationDbContext _db;
    private readonly ILectureAccessService _access;

    public StudentTestService(ApplicationDbContext db, ILectureAccessService access)
    {
        _db = db;
        _access = access;
    }

    private record TestContext(
        int TestId,
        bool IsPublished,
        int LectureId,
        string LectureTitle,
        string DisciplineName,
        int PassingPercentage,
        int MaxAttempts);

    // ===== Временный список тестов для страницы студента (на этапе 13 заменим кабинетом) =====

    public async Task<List<AvailableTestViewModel>> GetAvailableTestsAsync(string studentId)
    {
        return await _db.Tests
            .AsNoTracking()
            .Where(t => t.IsPublished
                        && t.Lecture.IsPublished
                        && t.Lecture.Topic.Discipline.IsPublished
                        && t.Questions.Any())
            .OrderBy(t => t.Lecture.Topic.Discipline.Name)
            .ThenBy(t => t.Lecture.Topic.OrderIndex)
            .ThenBy(t => t.Lecture.OrderIndex)
            .Select(t => new AvailableTestViewModel
            {
                LectureId = t.LectureId,
                LectureTitle = t.Lecture.Title,
                TopicName = t.Lecture.Topic.Name,
                DisciplineName = t.Lecture.Topic.Discipline.Name,
                QuestionsCount = t.Questions.Count,
                PassingPercentage = t.Lecture.PassingPercentage,
                MaxAttempts = t.Lecture.MaxAttempts,
                AttemptsUsed = t.Attempts.Count(a => a.StudentId == studentId),
                IsPassed = t.Attempts.Any(a => a.StudentId == studentId && a.IsPassed)
            })
            .ToListAsync();
    }

    // ===== Обзор теста =====

    public async Task<TestResponse<StudentTestOverviewViewModel>> GetOverviewAsync(string studentId, int lectureId)
    {
        var denied = await CheckAccessAsync(studentId, lectureId);
        if (denied != null)
            return new(denied.Value);

        var context = await GetContextAsync(lectureId);
        if (context == null || !context.IsPublished)
            return new(TestOutcome.NoTest);

        var questionsCount = await _db.TestQuestions.CountAsync(q => q.TestId == context.TestId);
        if (questionsCount == 0)
            return new(TestOutcome.NoTest);

        var attempts = await _db.TestAttempts
            .AsNoTracking()
            .Where(a => a.StudentId == studentId && a.TestId == context.TestId)
            .OrderBy(a => a.AttemptNumber)
            .ToListAsync();

        var completed = attempts.Where(a => a.CompletedAt != null).ToList();
        var isPassed = completed.Any(a => a.IsPassed);
        var hasInProgress = attempts.Any(a => a.CompletedAt == null);

        var model = new StudentTestOverviewViewModel
        {
            LectureId = lectureId,
            LectureTitle = context.LectureTitle,
            DisciplineName = context.DisciplineName,
            QuestionsCount = questionsCount,
            PassingPercentage = context.PassingPercentage,
            MaxAttempts = context.MaxAttempts,
            AttemptsUsed = attempts.Count,
            HasInProgress = hasInProgress,
            IsPassed = isPassed,
            CanStart = !isPassed && (hasInProgress || attempts.Count < context.MaxAttempts),
            History = completed.Select(ToHistoryRow).ToList()
        };

        return new(TestOutcome.Ok, model);
    }

    // ===== Начало попытки =====

    public async Task<TestResponse<int>> StartAsync(string studentId, int lectureId)
    {
        var denied = await CheckAccessAsync(studentId, lectureId);
        if (denied != null)
            return new(denied.Value);

        var context = await GetContextAsync(lectureId);
        if (context == null || !context.IsPublished)
            return new(TestOutcome.NoTest);

        var questionsCount = await _db.TestQuestions.CountAsync(q => q.TestId == context.TestId);
        if (questionsCount == 0)
            return new(TestOutcome.NoTest);

        var attempts = await _db.TestAttempts
            .AsNoTracking()
            .Where(a => a.StudentId == studentId && a.TestId == context.TestId)
            .Select(a => new { a.Id, a.AttemptNumber, a.CompletedAt, a.IsPassed })
            .ToListAsync();

        // Тест уже пройден: повторять не нужно
        if (attempts.Any(a => a.CompletedAt != null && a.IsPassed))
            return new(TestOutcome.AlreadyPassed);

        // Есть незавершённая попытка: продолжаем её, новую не создаём
        var inProgress = attempts.FirstOrDefault(a => a.CompletedAt == null);
        if (inProgress != null)
            return new(TestOutcome.Ok, inProgress.Id);

        if (attempts.Count >= context.MaxAttempts)
            return new(TestOutcome.AttemptsExhausted);

        var attempt = new TestAttempt
        {
            StudentId = studentId,
            TestId = context.TestId,
            StartedAt = DateTime.UtcNow,
            AttemptNumber = (attempts.Count == 0 ? 0 : attempts.Max(a => a.AttemptNumber)) + 1,
            TotalQuestions = questionsCount
        };

        _db.TestAttempts.Add(attempt);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Двойное нажатие "Начать": уникальный индекс не дал создать дубль попытки.
            // Находим уже созданную незавершённую попытку и продолжаем её.
            _db.Entry(attempt).State = EntityState.Detached;

            var existing = await _db.TestAttempts
                .AsNoTracking()
                .Where(a => a.StudentId == studentId && a.TestId == context.TestId && a.CompletedAt == null)
                .Select(a => (int?)a.Id)
                .FirstOrDefaultAsync();

            if (existing == null)
                throw;

            return new(TestOutcome.Ok, existing.Value);
        }

        return new(TestOutcome.Ok, attempt.Id);
    }

    // ===== Страница с вопросами =====

    public async Task<TestResponse<StudentTestTakeViewModel>> GetTakeAsync(string studentId, int attemptId)
    {
        var attempt = await _db.TestAttempts
            .AsNoTracking()
            .Where(a => a.Id == attemptId)
            .Select(a => new
            {
                a.StudentId,
                a.TestId,
                a.AttemptNumber,
                a.CompletedAt,
                LectureId = a.Test.LectureId
            })
            .FirstOrDefaultAsync();

        if (attempt == null)
            return new(TestOutcome.NotFound);

        // Чужая попытка
        if (attempt.StudentId != studentId)
            return new(TestOutcome.Forbidden);

        if (attempt.CompletedAt != null)
            return new(TestOutcome.AlreadyCompleted);

        var denied = await CheckAccessAsync(studentId, attempt.LectureId);
        if (denied != null)
            return new(denied.Value);

        var context = await GetContextAsync(attempt.LectureId);
        if (context == null || !context.IsPublished)
            return new(TestOutcome.NoTest);

        // В браузер уходят только Id и текст, без признака "правильный"
        var questions = await _db.TestQuestions
            .AsNoTracking()
            .Where(q => q.TestId == attempt.TestId)
            .OrderBy(q => q.OrderIndex).ThenBy(q => q.Id)
            .Select(q => new TakeQuestionViewModel
            {
                Id = q.Id,
                Text = q.Text,
                Options = q.Options
                    .OrderBy(o => o.OrderIndex).ThenBy(o => o.Id)
                    .Select(o => new TakeOptionViewModel { Id = o.Id, Text = o.Text })
                    .ToList()
            })
            .ToListAsync();

        if (questions.Count == 0)
            return new(TestOutcome.NoTest);

        for (var i = 0; i < questions.Count; i++)
            questions[i].Number = i + 1;

        var model = new StudentTestTakeViewModel
        {
            AttemptId = attemptId,
            LectureId = context.LectureId,
            LectureTitle = context.LectureTitle,
            DisciplineName = context.DisciplineName,
            AttemptNumber = attempt.AttemptNumber,
            MaxAttempts = context.MaxAttempts,
            PassingPercentage = context.PassingPercentage,
            Questions = questions
        };

        return new(TestOutcome.Ok, model);
    }

    // ===== Отправка ответов: проверка и расчёт результата =====

    public async Task<TestResponse<int>> SubmitAsync(string studentId, int attemptId, Dictionary<int, int> answers)
    {
        var attempt = await _db.TestAttempts
            .Include(a => a.Test)
            .FirstOrDefaultAsync(a => a.Id == attemptId);

        if (attempt == null)
            return new(TestOutcome.NotFound);

        if (attempt.StudentId != studentId)
            return new(TestOutcome.Forbidden);

        // Повторная отправка: результат уже посчитан, ничего не меняем
        if (attempt.CompletedAt != null)
            return new(TestOutcome.AlreadyCompleted);

        var denied = await CheckAccessAsync(studentId, attempt.Test.LectureId);
        if (denied != null)
            return new(denied.Value);

        var context = await GetContextAsync(attempt.Test.LectureId);
        if (context == null || !context.IsPublished)
            return new(TestOutcome.NoTest);

        // Правильные ответы берём ТОЛЬКО из базы данных
        var questions = await _db.TestQuestions
            .AsNoTracking()
            .Where(q => q.TestId == attempt.TestId)
            .Select(q => new
            {
                q.Id,
                CorrectOptionIds = q.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToList()
            })
            .ToListAsync();

        var total = questions.Count;
        var correct = 0;

        foreach (var question in questions)
        {
            // Вопрос засчитывается, только если выбран правильный вариант ИМЕННО этого вопроса.
            // Неотвеченный вопрос или чужой вариант считается неверным.
            if (answers.TryGetValue(question.Id, out var selectedOptionId) &&
                question.CorrectOptionIds.Contains(selectedOptionId))
            {
                correct++;
            }
        }

        attempt.CorrectAnswers = correct;
        attempt.TotalQuestions = total;
        attempt.ScorePercentage = total == 0 ? 0 : Math.Round(correct * 100.0 / total, 1);
        // Целочисленное сравнение: без ошибок округления
        attempt.IsPassed = total > 0 && correct * 100 >= context.PassingPercentage * total;
        attempt.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return new(TestOutcome.Ok, attempt.Id);
    }

    // ===== Результат =====

    public async Task<TestResponse<StudentTestResultViewModel>> GetResultAsync(string studentId, int attemptId)
    {
        var attempt = await _db.TestAttempts
            .AsNoTracking()
            .Where(a => a.Id == attemptId)
            .Select(a => new
            {
                a.Id,
                a.StudentId,
                a.TestId,
                a.AttemptNumber,
                a.CorrectAnswers,
                a.TotalQuestions,
                a.ScorePercentage,
                a.IsPassed,
                a.CompletedAt,
                LectureId = a.Test.LectureId
            })
            .FirstOrDefaultAsync();

        if (attempt == null)
            return new(TestOutcome.NotFound);

        // Студент может смотреть только собственные результаты
        if (attempt.StudentId != studentId)
            return new(TestOutcome.Forbidden);

        // Результата незавершённой попытки ещё нет
        if (attempt.CompletedAt == null)
            return new(TestOutcome.NotFound);

        var context = await GetContextAsync(attempt.LectureId);
        if (context == null)
            return new(TestOutcome.NotFound);

        var attempts = await _db.TestAttempts
            .AsNoTracking()
            .Where(a => a.StudentId == studentId && a.TestId == attempt.TestId)
            .OrderBy(a => a.AttemptNumber)
            .ToListAsync();

        var used = attempts.Count;
        var passedAny = attempts.Any(a => a.CompletedAt != null && a.IsPassed);

        var model = new StudentTestResultViewModel
        {
            AttemptId = attempt.Id,
            LectureId = context.LectureId,
            LectureTitle = context.LectureTitle,
            DisciplineName = context.DisciplineName,
            AttemptNumber = attempt.AttemptNumber,
            CorrectAnswers = attempt.CorrectAnswers,
            TotalQuestions = attempt.TotalQuestions,
            ScorePercentage = attempt.ScorePercentage,
            PassingPercentage = context.PassingPercentage,
            IsPassed = attempt.IsPassed,
            CompletedAt = attempt.CompletedAt.Value,
            MaxAttempts = context.MaxAttempts,
            AttemptsUsed = used,
            CanRetry = !passedAny && context.IsPublished && used < context.MaxAttempts,
            History = attempts.Where(a => a.CompletedAt != null).Select(ToHistoryRow).ToList()
        };

        return new(TestOutcome.Ok, model);
    }

    // ===== Вспомогательные методы =====

    private async Task<TestContext?> GetContextAsync(int lectureId)
    {
        return await _db.Tests
            .AsNoTracking()
            .Where(t => t.LectureId == lectureId)
            .Select(t => new TestContext(
                t.Id,
                t.IsPublished,
                t.LectureId,
                t.Lecture.Title,
                t.Lecture.Topic.Discipline.Name,
                t.Lecture.PassingPercentage,
                t.Lecture.MaxAttempts))
            .FirstOrDefaultAsync();
    }

    // Возвращает причину отказа или null, если доступ разрешён
    private async Task<TestOutcome?> CheckAccessAsync(string studentId, int lectureId)
    {
        var access = await _access.CheckAsync(studentId, lectureId);

        return access.Status switch
        {
            LectureAccessStatus.Allowed => null,
            LectureAccessStatus.NotFound => TestOutcome.NotFound,
            _ => TestOutcome.LectureLocked
        };
    }

    private static AttemptHistoryRowViewModel ToHistoryRow(TestAttempt a) => new()
    {
        AttemptId = a.Id,
        AttemptNumber = a.AttemptNumber,
        CompletedAt = a.CompletedAt ?? a.StartedAt,
        CorrectAnswers = a.CorrectAnswers,
        TotalQuestions = a.TotalQuestions,
        ScorePercentage = a.ScorePercentage,
        IsPassed = a.IsPassed
    };
}