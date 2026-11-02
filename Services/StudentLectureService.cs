using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Models;
using ElectronicStudyCabinet.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ElectronicStudyCabinet.Services;

public interface IStudentLectureService
{
    Task<StudentDisciplineViewModel?> GetDisciplineAsync(string studentId, int disciplineId);
    Task<StudentLecturePageViewModel?> GetLecturePageAsync(string studentId, int lectureId);
}

public class StudentLectureService : IStudentLectureService
{
    private readonly ApplicationDbContext _db;
    private readonly ILectureAccessService _access;

    public StudentLectureService(ApplicationDbContext db, ILectureAccessService access)
    {
        _db = db;
        _access = access;
    }


    // Структура дисциплины: Дисциплина → Темы → Лекции со статусами
    public async Task<StudentDisciplineViewModel?> GetDisciplineAsync(string studentId, int disciplineId)
    {
        var discipline = await _db.Disciplines
            .AsNoTracking()
            .Where(d => d.Id == disciplineId && d.IsPublished)
            .Select(d => new { d.Id, d.Name, d.Description })
            .FirstOrDefaultAsync();

        if (discipline == null)
            return null;

        var states = await _access.GetDisciplineStatesAsync(studentId, disciplineId);
        var completed = states.Count(s => s.Status == LectureStatus.Completed);

        return new StudentDisciplineViewModel
        {
            Id = discipline.Id,
            Name = discipline.Name,
            Description = discipline.Description,
            TotalLectures = states.Count,
            CompletedLectures = completed,
            ProgressPercent = states.Count == 0 ? 0 : (int)Math.Round(completed * 100.0 / states.Count),
            Topics = states
                .GroupBy(s => s.TopicId)
                .Select(g => new StudentTopicViewModel
                {
                    TopicId = g.Key,
                    Name = g.First().TopicName,
                    Lectures = g.ToList()
                })
                .ToList()
        };
    }

    // Страница лекции (доступ к ней уже проверен контроллером)
    public async Task<StudentLecturePageViewModel?> GetLecturePageAsync(string studentId, int lectureId)
    {
        var info = await _db.Lectures
            .AsNoTracking()
            .Where(l => l.Id == lectureId)
            .Select(l => new
            {
                l.Topic.DisciplineId,
                DisciplineName = l.Topic.Discipline.Name,
                l.RequireControlQuestions
            })
            .FirstOrDefaultAsync();

        if (info == null)
            return null;

        var states = await _access.GetDisciplineStatesAsync(studentId, info.DisciplineId);
        var index = states.FindIndex(s => s.LectureId == lectureId);
        if (index < 0)
            return null;

        var questions = await _db.ControlQuestions
            .AsNoTracking()
            .Where(q => q.LectureId == lectureId)
            .OrderBy(q => q.OrderIndex).ThenBy(q => q.Id)
            .Select(q => new LectureQuestionViewModel
            {
                Id = q.Id,
                Text = q.Text,
                QuestionType = q.QuestionType,
                IsRequired = q.IsRequired,
                RequiresManualReview = q.RequiresManualReview,
                Options = q.Options
                    .OrderBy(o => o.OrderIndex).ThenBy(o => o.Id)
                    .Select(o => new TakeOptionViewModel { Id = o.Id, Text = o.Text })
                    .ToList()
            })
            .ToListAsync();

        var answers = await _db.StudentAnswers
            .AsNoTracking()
            .Where(a => a.StudentId == studentId && a.ControlQuestion.LectureId == lectureId)
            .ToListAsync();

        for (var i = 0; i < questions.Count; i++)
        {
            var question = questions[i];
            question.Number = i + 1;

            var answer = answers.FirstOrDefault(a => a.ControlQuestionId == question.Id);
            if (answer != null)
            {
                question.HasAnswer = true;
                question.AnswerText = answer.AnswerText;
                question.SelectedOptionId = answer.SelectedOptionId;
                question.IsCorrect = answer.IsCorrect;
                question.ReviewStatus = answer.ReviewStatus;
            }

            // Отправить ответ можно, если его ещё нет, он неверный или отклонён
            question.CanAnswer = answer == null
                                 || answer.ReviewStatus == ReviewStatus.Rejected
                                 || (answer.ReviewStatus == ReviewStatus.AutoChecked && !answer.IsCorrect);
        }

        var state = states[index];

        var attemptsUsed = state.TestRequired
            ? await _db.TestAttempts.CountAsync(a => a.StudentId == studentId && a.Test.LectureId == lectureId)
            : 0;

        return new StudentLecturePageViewModel
        {
            State = state,
            DisciplineId = info.DisciplineId,
            DisciplineName = info.DisciplineName,
            RequireControlQuestions = info.RequireControlQuestions,
            Questions = questions,
            TestAttemptsUsed = attemptsUsed,
            Next = index + 1 < states.Count ? states[index + 1] : null
        };
    }
}