using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Models;
using ElectronicStudyCabinet.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStudyCabinet.Services;

public interface ITestService
{
    Task<TestPageViewModel?> GetPageAsync(int lectureId);
    Task<bool> CreateAsync(int lectureId);
    Task<(bool Success, string? Error)> SetPublishedAsync(int lectureId, bool publish);
    Task<DeleteResult> DeleteAsync(int lectureId);

    Task<TestQuestionFormViewModel?> GetNewQuestionAsync(int lectureId);
    Task<TestQuestionFormViewModel?> GetQuestionFormAsync(int id);
    Task FillContextAsync(TestQuestionFormViewModel model);
    List<KeyValuePair<string, string>> ValidateQuestion(TestQuestionFormViewModel model);
    Task<bool> CreateQuestionAsync(TestQuestionFormViewModel model);
    Task<int?> UpdateQuestionAsync(TestQuestionFormViewModel model);
    Task<(DeleteResult Result, int? LectureId)> DeleteQuestionAsync(int id);
}

public class TestService : ITestService
{
    private readonly ApplicationDbContext _db;

    public TestService(ApplicationDbContext db)
    {
        _db = db;
    }

    // ===== Тест =====

    public async Task<TestPageViewModel?> GetPageAsync(int lectureId)
    {
        var page = await _db.Lectures
            .AsNoTracking()
            .Where(l => l.Id == lectureId)
            .Select(l => new TestPageViewModel
            {
                LectureId = l.Id,
                LectureTitle = l.Title,
                TopicName = l.Topic.Name,
                DisciplineId = l.Topic.DisciplineId,
                DisciplineName = l.Topic.Discipline.Name,
                PassingPercentage = l.PassingPercentage,
                MaxAttempts = l.MaxAttempts
            })
            .FirstOrDefaultAsync();

        if (page == null)
            return null;

        var test = await _db.Tests
            .AsNoTracking()
            .Where(t => t.LectureId == lectureId)
            .Select(t => new { t.Id, t.IsPublished })
            .FirstOrDefaultAsync();

        if (test == null)
            return page;

        page.HasTest = true;
        page.IsPublished = test.IsPublished;
        page.AttemptsCount = await _db.TestAttempts.CountAsync(a => a.TestId == test.Id);

        page.Questions = await _db.TestQuestions
            .AsNoTracking()
            .Where(q => q.TestId == test.Id)
            .OrderBy(q => q.OrderIndex).ThenBy(q => q.Id)
            .Select(q => new TestQuestionRowViewModel
            {
                Id = q.Id,
                Text = q.Text,
                OrderIndex = q.OrderIndex,
                Options = q.Options
                    .OrderBy(o => o.OrderIndex).ThenBy(o => o.Id)
                    .Select(o => new TestOptionRowViewModel { Text = o.Text, IsCorrect = o.IsCorrect })
                    .ToList()
            })
            .ToListAsync();

        return page;
    }

    public async Task<bool> CreateAsync(int lectureId)
    {
        if (!await _db.Lectures.AnyAsync(l => l.Id == lectureId))
            return false;

        // Тест уже есть: ничего не делаем
        if (await _db.Tests.AnyAsync(t => t.LectureId == lectureId))
            return true;

        _db.Tests.Add(new Test { LectureId = lectureId, IsPublished = false });

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Двойная отправка формы: уникальный индекс в базе не дал создать второй тест
        }

        return true;
    }

    public async Task<(bool Success, string? Error)> SetPublishedAsync(int lectureId, bool publish)
    {
        var test = await _db.Tests.FirstOrDefaultAsync(t => t.LectureId == lectureId);
        if (test == null)
            return (false, "Тест не найден.");

        if (publish)
        {
            var questions = await _db.TestQuestions
                .Where(q => q.TestId == test.Id)
                .Select(q => new
                {
                    OptionsCount = q.Options.Count,
                    CorrectCount = q.Options.Count(o => o.IsCorrect)
                })
                .ToListAsync();

            if (questions.Count == 0)
                return (false, "Нельзя опубликовать тест без вопросов.");

            if (questions.Any(q => q.OptionsCount < 2 || q.CorrectCount != 1))
                return (false, "В каждом вопросе должно быть не менее двух вариантов и ровно один правильный.");
        }

        test.IsPublished = publish;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<DeleteResult> DeleteAsync(int lectureId)
    {
        var test = await _db.Tests.FirstOrDefaultAsync(t => t.LectureId == lectureId);
        if (test == null)
            return DeleteResult.NotFound;

        // Нельзя удалить тест, который студенты уже проходили
        if (await _db.TestAttempts.AnyAsync(a => a.TestId == test.Id))
            return DeleteResult.HasChildren;

        // Вопросы и варианты ответов удалятся вместе с тестом (Cascade)
        _db.Tests.Remove(test);
        await _db.SaveChangesAsync();
        return DeleteResult.Deleted;
    }

    // ===== Вопросы теста =====

    // Пустая форма для нового вопроса (тест должен существовать)
    public async Task<TestQuestionFormViewModel?> GetNewQuestionAsync(int lectureId)
    {
        var test = await _db.Tests
            .AsNoTracking()
            .Where(t => t.LectureId == lectureId)
            .Select(t => new { t.Id })
            .FirstOrDefaultAsync();

        if (test == null)
            return null;

        var info = await GetLectureInfoAsync(lectureId);
        if (info == null)
            return null;

        var maxOrder = await _db.TestQuestions
            .Where(q => q.TestId == test.Id)
            .MaxAsync(q => (int?)q.OrderIndex) ?? 0;

        var model = new TestQuestionFormViewModel
        {
            LectureId = lectureId,
            LectureTitle = info.Title,
            DisciplineId = info.DisciplineId,
            DisciplineName = info.DisciplineName,
            OrderIndex = maxOrder + 1
        };
        model.Normalize();
        return model;
    }

    public async Task<TestQuestionFormViewModel?> GetQuestionFormAsync(int id)
    {
        var question = await _db.TestQuestions
            .AsNoTracking()
            .Include(q => q.Options)
            .Include(q => q.Test)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null)
            return null;

        var lectureId = question.Test.LectureId;
        var info = await GetLectureInfoAsync(lectureId);

        var model = new TestQuestionFormViewModel
        {
            Id = question.Id,
            LectureId = lectureId,
            LectureTitle = info?.Title,
            DisciplineId = info?.DisciplineId ?? 0,
            DisciplineName = info?.DisciplineName,
            Text = question.Text,
            OrderIndex = question.OrderIndex
        };

        // Варианты раскладываем по полям формы подряд: 0, 1, 2...
        var options = question.Options.OrderBy(o => o.OrderIndex).ThenBy(o => o.Id).ToList();
        for (var i = 0; i < options.Count && i < TestQuestionFormViewModel.OptionSlots; i++)
        {
            model.Options.Add(new TestOptionInput { Text = options[i].Text });
            if (options[i].IsCorrect)
                model.CorrectOptionIndex = i;
        }

        model.Normalize();
        return model;
    }

    // Заново заполняет поля для показа, если форма вернулась с ошибкой
    public async Task FillContextAsync(TestQuestionFormViewModel model)
    {
        var info = await GetLectureInfoAsync(model.LectureId);
        if (info != null)
        {
            model.LectureTitle = info.Title;
            model.DisciplineId = info.DisciplineId;
            model.DisciplineName = info.DisciplineName;
        }
    }

    // Правила для вариантов ответа. Ключ "" = ошибка всей формы.
    public List<KeyValuePair<string, string>> ValidateQuestion(TestQuestionFormViewModel model)
    {
        var errors = new List<KeyValuePair<string, string>>();

        var filled = model.Options
            .Select((option, index) => new { option, index })
            .Where(x => !string.IsNullOrWhiteSpace(x.option.Text))
            .ToList();

        if (filled.Count < 2)
        {
            errors.Add(new(string.Empty, "Укажите не менее двух вариантов ответа"));
        }
        else if (model.CorrectOptionIndex == null || !filled.Any(x => x.index == model.CorrectOptionIndex))
        {
            errors.Add(new(string.Empty, "Отметьте правильный вариант ответа (он должен быть заполнен)"));
        }

        return errors;
    }

    public async Task<bool> CreateQuestionAsync(TestQuestionFormViewModel model)
    {
        var test = await _db.Tests.FirstOrDefaultAsync(t => t.LectureId == model.LectureId);
        if (test == null)
            return false;

        _db.TestQuestions.Add(new TestQuestion
        {
            TestId = test.Id,
            Text = model.Text.Trim(),
            OrderIndex = model.OrderIndex,
            Options = BuildOptions(model)
        });

        await _db.SaveChangesAsync();
        return true;
    }

    // Возвращает Id лекции или null, если вопрос не найден
    public async Task<int?> UpdateQuestionAsync(TestQuestionFormViewModel model)
    {
        var question = await _db.TestQuestions
            .Include(q => q.Options)
            .Include(q => q.Test)
            .FirstOrDefaultAsync(q => q.Id == model.Id);

        if (question == null)
            return null;

        question.Text = model.Text.Trim();
        question.OrderIndex = model.OrderIndex;

        // Старые варианты заменяем новыми (на варианты ссылаться больше нечему)
        _db.AnswerOptions.RemoveRange(question.Options);
        foreach (var option in BuildOptions(model))
        {
            option.TestQuestionId = question.Id;
            _db.AnswerOptions.Add(option);
        }

        await _db.SaveChangesAsync();
        return question.Test.LectureId;
    }

    public async Task<(DeleteResult Result, int? LectureId)> DeleteQuestionAsync(int id)
    {
        var question = await _db.TestQuestions
            .Include(q => q.Test)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null)
            return (DeleteResult.NotFound, null);

        var lectureId = question.Test.LectureId;

        // Нельзя удалить последний вопрос опубликованного теста
        if (question.Test.IsPublished &&
            await _db.TestQuestions.CountAsync(q => q.TestId == question.TestId) <= 1)
        {
            return (DeleteResult.HasChildren, lectureId);
        }

        _db.TestQuestions.Remove(question);
        await _db.SaveChangesAsync();
        return (DeleteResult.Deleted, lectureId);
    }

    // ===== Вспомогательные методы =====

    private static List<AnswerOption> BuildOptions(TestQuestionFormViewModel model)
    {
        var result = new List<AnswerOption>();
        var order = 0;

        for (var slot = 0; slot < model.Options.Count; slot++)
        {
            var text = model.Options[slot].Text?.Trim();
            if (string.IsNullOrEmpty(text))
                continue;

            order++;
            result.Add(new AnswerOption
            {
                Text = text,
                IsCorrect = slot == model.CorrectOptionIndex,
                OrderIndex = order
            });
        }

        return result;
    }

    private async Task<LectureInfo?> GetLectureInfoAsync(int lectureId)
    {
        return await _db.Lectures
            .AsNoTracking()
            .Where(l => l.Id == lectureId)
            .Select(l => new LectureInfo(l.Title, l.Topic.DisciplineId, l.Topic.Discipline.Name))
            .FirstOrDefaultAsync();
    }

    private record LectureInfo(string Title, int DisciplineId, string DisciplineName);
}