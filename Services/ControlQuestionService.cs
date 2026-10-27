using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Models;
using ElectronicStudyCabinet.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStudyCabinet.Services;

public interface IControlQuestionService
{
    Task<ControlQuestionListViewModel?> GetListAsync(int lectureId);
    Task<ControlQuestionFormViewModel?> GetNewAsync(int lectureId);
    Task<ControlQuestionFormViewModel?> GetFormAsync(int id);
    Task FillContextAsync(ControlQuestionFormViewModel model);
    Task<List<KeyValuePair<string, string>>> ValidateAsync(ControlQuestionFormViewModel model);
    Task<bool> CreateAsync(ControlQuestionFormViewModel model);
    Task<int?> UpdateAsync(ControlQuestionFormViewModel model);
    Task<DeleteResult> DeleteAsync(int id);
}

public class ControlQuestionService : IControlQuestionService
{
    private readonly ApplicationDbContext _db;

    public ControlQuestionService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ControlQuestionListViewModel?> GetListAsync(int lectureId)
    {
        var lecture = await _db.Lectures
            .AsNoTracking()
            .Where(l => l.Id == lectureId)
            .Select(l => new ControlQuestionListViewModel
            {
                LectureId = l.Id,
                LectureTitle = l.Title,
                TopicName = l.Topic.Name,
                DisciplineId = l.Topic.DisciplineId,
                DisciplineName = l.Topic.Discipline.Name,
                RequireControlQuestions = l.RequireControlQuestions
            })
            .FirstOrDefaultAsync();

        if (lecture == null)
            return null;

        lecture.Items = await _db.ControlQuestions
            .AsNoTracking()
            .Where(q => q.LectureId == lectureId)
            .OrderBy(q => q.OrderIndex).ThenBy(q => q.Id)
            .Select(q => new ControlQuestionRowViewModel
            {
                Id = q.Id,
                Text = q.Text,
                QuestionType = q.QuestionType,
                IsRequired = q.IsRequired,
                RequiresManualReview = q.RequiresManualReview,
                OrderIndex = q.OrderIndex,
                CorrectAnswerDisplay = q.QuestionType == QuestionType.SingleChoice
                    ? q.Options.Where(o => o.IsCorrect).Select(o => o.Text).FirstOrDefault()
                    : q.CorrectAnswer
            })
            .ToListAsync();

        return lecture;
    }

    // Пустая форма для нового вопроса
    public async Task<ControlQuestionFormViewModel?> GetNewAsync(int lectureId)
    {
        var info = await GetLectureInfoAsync(lectureId);
        if (info == null)
            return null;

        var maxOrder = await _db.ControlQuestions
            .Where(q => q.LectureId == lectureId)
            .MaxAsync(q => (int?)q.OrderIndex) ?? 0;

        var model = new ControlQuestionFormViewModel
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

    public async Task<ControlQuestionFormViewModel?> GetFormAsync(int id)
    {
        var question = await _db.ControlQuestions
            .AsNoTracking()
            .Include(q => q.Options)
            .Where(q => q.Id == id)
            .FirstOrDefaultAsync();

        if (question == null)
            return null;

        var info = await GetLectureInfoAsync(question.LectureId);

        var model = new ControlQuestionFormViewModel
        {
            Id = question.Id,
            LectureId = question.LectureId,
            LectureTitle = info?.Title,
            DisciplineId = info?.DisciplineId ?? 0,
            DisciplineName = info?.DisciplineName,
            Text = question.Text,
            QuestionType = question.QuestionType,
            CorrectAnswer = question.CorrectAnswer,
            IsRequired = question.IsRequired,
            RequiresManualReview = question.RequiresManualReview,
            OrderIndex = question.OrderIndex
        };

        // Варианты раскладываем по полям формы подряд: 0, 1, 2...
        var options = question.Options.OrderBy(o => o.OrderIndex).ThenBy(o => o.Id).ToList();
        for (var i = 0; i < options.Count && i < ControlQuestionFormViewModel.OptionSlots; i++)
        {
            model.Options.Add(new ControlQuestionOptionInput { Id = options[i].Id, Text = options[i].Text });
            if (options[i].IsCorrect)
                model.CorrectOptionIndex = i;
        }

        model.Normalize();
        return model;
    }

    // Заново заполняет поля для показа, если форма вернулась с ошибкой
    public async Task FillContextAsync(ControlQuestionFormViewModel model)
    {
        var info = await GetLectureInfoAsync(model.LectureId);
        if (info != null)
        {
            model.LectureTitle = info.Title;
            model.DisciplineId = info.DisciplineId;
            model.DisciplineName = info.DisciplineName;
        }
    }

    // Проверка правил, которые нельзя выразить атрибутами. Ключ "" = ошибка всей формы.
    public async Task<List<KeyValuePair<string, string>>> ValidateAsync(ControlQuestionFormViewModel model)
    {
        var errors = new List<KeyValuePair<string, string>>();

        if (model.QuestionType == QuestionType.ShortText)
        {
            if (!model.RequiresManualReview && string.IsNullOrWhiteSpace(model.CorrectAnswer))
            {
                errors.Add(new(nameof(model.CorrectAnswer),
                    "Укажите правильный ответ или включите ручную проверку"));
            }
        }
        else if (model.QuestionType == QuestionType.SingleChoice)
        {
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
        }
        else
        {
            errors.Add(new(nameof(model.QuestionType), "Выберите тип вопроса"));
        }

        // Тип вопроса нельзя менять, если на него уже отвечали студенты
        if (model.Id > 0)
        {
            var currentType = await _db.ControlQuestions
                .AsNoTracking()
                .Where(q => q.Id == model.Id)
                .Select(q => (QuestionType?)q.QuestionType)
                .FirstOrDefaultAsync();

            if (currentType != null && currentType != model.QuestionType &&
                await _db.StudentAnswers.AnyAsync(a => a.ControlQuestionId == model.Id))
            {
                errors.Add(new(nameof(model.QuestionType),
                    "Нельзя изменить тип вопроса: на него уже есть ответы студентов"));
            }
        }

        return errors;
    }

    public async Task<bool> CreateAsync(ControlQuestionFormViewModel model)
    {
        if (!await _db.Lectures.AnyAsync(l => l.Id == model.LectureId))
            return false;

        var isShortText = model.QuestionType == QuestionType.ShortText;

        var question = new ControlQuestion
        {
            LectureId = model.LectureId,
            Text = model.Text.Trim(),
            QuestionType = model.QuestionType,
            CorrectAnswer = isShortText && !string.IsNullOrWhiteSpace(model.CorrectAnswer)
                ? model.CorrectAnswer.Trim()
                : null,
            IsRequired = model.IsRequired,
            // Ручная проверка возможна только для текстовых ответов
            RequiresManualReview = isShortText && model.RequiresManualReview,
            OrderIndex = model.OrderIndex
        };

        if (!isShortText)
        {
            var order = 0;
            for (var slot = 0; slot < model.Options.Count; slot++)
            {
                var text = model.Options[slot].Text?.Trim();
                if (string.IsNullOrEmpty(text))
                    continue;

                order++;
                question.Options.Add(new ControlQuestionOption
                {
                    Text = text,
                    IsCorrect = slot == model.CorrectOptionIndex,
                    OrderIndex = order
                });
            }
        }

        _db.ControlQuestions.Add(question);
        await _db.SaveChangesAsync();
        return true;
    }

    // Возвращает Id лекции или null, если вопрос не найден
    public async Task<int?> UpdateAsync(ControlQuestionFormViewModel model)
    {
        var question = await _db.ControlQuestions
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == model.Id);

        if (question == null)
            return null;

        var isShortText = model.QuestionType == QuestionType.ShortText;

        // LectureId намеренно не меняем
        question.Text = model.Text.Trim();
        question.QuestionType = model.QuestionType;
        question.CorrectAnswer = isShortText && !string.IsNullOrWhiteSpace(model.CorrectAnswer)
            ? model.CorrectAnswer.Trim()
            : null;
        question.IsRequired = model.IsRequired;
        question.RequiresManualReview = isShortText && model.RequiresManualReview;
        question.OrderIndex = model.OrderIndex;

        if (isShortText)
        {
            // У текстового вопроса вариантов нет
            _db.ControlQuestionOptions.RemoveRange(question.Options);
        }
        else
        {
            // Существующие варианты обновляем (чтобы сохранить их Id), лишние удаляем
            var existing = question.Options.ToDictionary(o => o.Id);
            var kept = new HashSet<int>();
            var order = 0;

            for (var slot = 0; slot < model.Options.Count; slot++)
            {
                var text = model.Options[slot].Text?.Trim();
                if (string.IsNullOrEmpty(text))
                    continue;

                order++;
                var isCorrect = slot == model.CorrectOptionIndex;
                var postedId = model.Options[slot].Id;

                if (postedId != 0 && existing.TryGetValue(postedId, out var option) && kept.Add(postedId))
                {
                    option.Text = text;
                    option.IsCorrect = isCorrect;
                    option.OrderIndex = order;
                }
                else
                {
                    question.Options.Add(new ControlQuestionOption
                    {
                        Text = text,
                        IsCorrect = isCorrect,
                        OrderIndex = order
                    });
                }
            }

            var removed = question.Options.Where(o => o.Id != 0 && !kept.Contains(o.Id)).ToList();
            _db.ControlQuestionOptions.RemoveRange(removed);
        }

        await _db.SaveChangesAsync();
        return question.LectureId;
    }

    public async Task<DeleteResult> DeleteAsync(int id)
    {
        var question = await _db.ControlQuestions.FindAsync(id);
        if (question == null)
            return DeleteResult.NotFound;

        // Нельзя удалить вопрос, на который уже отвечали студенты
        if (await _db.StudentAnswers.AnyAsync(a => a.ControlQuestionId == id))
            return DeleteResult.HasChildren;

        // Варианты ответов удалятся вместе с вопросом (Cascade)
        _db.ControlQuestions.Remove(question);
        await _db.SaveChangesAsync();
        return DeleteResult.Deleted;
    }

    // ===== Вспомогательные методы =====

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