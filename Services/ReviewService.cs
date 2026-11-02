using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Models;
using ElectronicStudyCabinet.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStudyCabinet.Services;

public interface IReviewService
{
    Task<List<PendingReviewRowViewModel>> GetPendingAsync();
    Task<bool> ReviewAsync(int answerId, bool accept, string teacherId);
    Task<int> CountPendingAsync();
}

public class ReviewService : IReviewService
{
    private readonly ApplicationDbContext _db;
    private readonly ILectureProgressService _progress;

    public ReviewService(ApplicationDbContext db, ILectureProgressService progress)
    {
        _db = db;
        _progress = progress;
    }
    public async Task<int> CountPendingAsync()
    {
        return await _db.StudentAnswers.CountAsync(a => a.ReviewStatus == ReviewStatus.PendingReview);
    }
    public async Task<List<PendingReviewRowViewModel>> GetPendingAsync()
    {
        return await _db.StudentAnswers
            .AsNoTracking()
            .Where(a => a.ReviewStatus == ReviewStatus.PendingReview)
            .OrderBy(a => a.SubmittedAt)
            .Select(a => new PendingReviewRowViewModel
            {
                AnswerId = a.Id,
                StudentName = a.Student.FullName,
                DisciplineName = a.ControlQuestion.Lecture.Topic.Discipline.Name,
                LectureTitle = a.ControlQuestion.Lecture.Title,
                QuestionText = a.ControlQuestion.Text,
                ReferenceAnswer = a.ControlQuestion.CorrectAnswer,
                AnswerText = a.AnswerText,
                SubmittedAt = a.SubmittedAt
            })
            .ToListAsync();
    }

    // Принять или отклонить ответ. false: ответ не найден или уже проверен.
    public async Task<bool> ReviewAsync(int answerId, bool accept, string teacherId)
    {
        var answer = await _db.StudentAnswers
            .Include(a => a.ControlQuestion)
            .Include(a => a.Student)
            .FirstOrDefaultAsync(a => a.Id == answerId);

        if (answer == null || answer.ReviewStatus != ReviewStatus.PendingReview)
            return false;

        answer.ReviewStatus = accept ? ReviewStatus.Accepted : ReviewStatus.Rejected;
        answer.IsCorrect = accept;
        answer.ReviewedAt = DateTime.UtcNow;

        // Запись в журнал действий преподавателя
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = teacherId,
            Action = accept ? AuditActions.ReviewAccepted : AuditActions.ReviewRejected,
            Description = $"Студент {answer.Student.FullName}: ответ на вопрос №{answer.ControlQuestionId} " +
                          (accept ? "принят" : "отклонён"),
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        // Проверка могла завершить лекцию и открыть следующую
        await _progress.SyncAsync(answer.StudentId, answer.ControlQuestion.LectureId);
        return true;
    }
}