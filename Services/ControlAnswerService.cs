using System.Text.RegularExpressions;
using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Models;
using ElectronicStudyCabinet.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStudyCabinet.Services;

public enum AnswerOutcome
{
    Correct,         // верно
    Incorrect,       // неверно, можно повторить
    SentForReview,   // отправлено на проверку преподавателю
    AlreadyAccepted, // ответ уже принят
    AlreadyPending,  // ответ уже ждёт проверки
    Duplicate,       // повторная отправка формы
    Invalid,         // пустой или некорректный ответ
    Locked,          // лекция недоступна
    NotFound
}

public interface IControlAnswerService
{
    Task<AnswerOutcome> SubmitAsync(string studentId, LectureAnswerInput input);
}

public class ControlAnswerService : IControlAnswerService
{
    private readonly ApplicationDbContext _db;
    private readonly ILectureAccessService _access;
    private readonly ILectureProgressService _progress;

    public ControlAnswerService(
        ApplicationDbContext db,
        ILectureAccessService access,
        ILectureProgressService progress)
    {
        _db = db;
        _access = access;
        _progress = progress;
    }

    public async Task<AnswerOutcome> SubmitAsync(string studentId, LectureAnswerInput input)
    {
        // Отвечать можно только на вопросы лекции, к которой есть доступ
        var access = await _access.CheckAsync(studentId, input.LectureId);
        if (access.Status == LectureAccessStatus.NotFound)
            return AnswerOutcome.NotFound;
        if (!access.IsAllowed)
            return AnswerOutcome.Locked;

        // Вопрос должен принадлежать именно этой лекции
        var question = await _db.ControlQuestions
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == input.QuestionId && q.LectureId == input.LectureId);

        if (question == null)
            return AnswerOutcome.NotFound;

        string answerText;
        int? selectedOptionId = null;
        bool isCorrect;
        ReviewStatus review;

        if (question.QuestionType == QuestionType.SingleChoice)
        {
            // Выбранный вариант обязан принадлежать этому вопросу
            var option = question.Options.FirstOrDefault(o => o.Id == input.SelectedOptionId);
            if (option == null)
                return AnswerOutcome.Invalid;

            answerText = option.Text;
            selectedOptionId = option.Id;
            isCorrect = option.IsCorrect;
            review = ReviewStatus.AutoChecked;
        }
        else
        {
            var text = input.AnswerText?.Trim();
            if (string.IsNullOrEmpty(text) || text.Length > 2000)
                return AnswerOutcome.Invalid;

            answerText = text;

            if (question.RequiresManualReview)
            {
                isCorrect = false;
                review = ReviewStatus.PendingReview;
            }
            else
            {
                isCorrect = !string.IsNullOrWhiteSpace(question.CorrectAnswer)
                            && Normalize(text) == Normalize(question.CorrectAnswer);
                review = ReviewStatus.AutoChecked;
            }
        }

        var existing = await _db.StudentAnswers
            .FirstOrDefaultAsync(a => a.StudentId == studentId && a.ControlQuestionId == question.Id);

        if (existing != null)
        {
            // Принятый ответ менять нельзя
            if (existing.ReviewStatus == ReviewStatus.Accepted ||
                (existing.ReviewStatus == ReviewStatus.AutoChecked && existing.IsCorrect))
                return AnswerOutcome.AlreadyAccepted;

            // Ответ ждёт проверки: пока преподаватель не проверит, менять нельзя
            if (existing.ReviewStatus == ReviewStatus.PendingReview)
                return AnswerOutcome.AlreadyPending;

            // Неверный или отклонённый ответ можно отправить заново: заменяем запись
            existing.AnswerText = answerText;
            existing.SelectedOptionId = selectedOptionId;
            existing.IsCorrect = isCorrect;
            existing.ReviewStatus = review;
            existing.SubmittedAt = DateTime.UtcNow;
            existing.ReviewedAt = null;
        }
        else
        {
            _db.StudentAnswers.Add(new StudentAnswer
            {
                StudentId = studentId,
                ControlQuestionId = question.Id,
                AnswerText = answerText,
                SelectedOptionId = selectedOptionId,
                IsCorrect = isCorrect,
                ReviewStatus = review,
                SubmittedAt = DateTime.UtcNow
            });
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Двойная отправка формы: уникальный индекс (студент + вопрос) не дал создать дубль
            _db.ChangeTracker.Clear();
            return AnswerOutcome.Duplicate;
        }

        await _progress.SyncAsync(studentId, input.LectureId);

        if (review == ReviewStatus.PendingReview)
            return AnswerOutcome.SentForReview;

        return isCorrect ? AnswerOutcome.Correct : AnswerOutcome.Incorrect;
    }

    // Простая нормализация: лишние пробелы, регистр и буква "ё" не влияют на проверку
    private static string Normalize(string value)
    {
        var collapsed = Regex.Replace(value.Trim(), @"\s+", " ");
        return collapsed.ToLowerInvariant().Replace('ё', 'е');
    }
}