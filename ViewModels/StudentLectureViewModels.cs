using System.ComponentModel.DataAnnotations;
using ElectronicStudyCabinet.Models;

namespace ElectronicStudyCabinet.ViewModels;

// Состояние одной лекции для конкретного студента
public class LectureStateViewModel
{
    public int LectureId { get; set; }
    public int TopicId { get; set; }
    public string TopicName { get; set; } = string.Empty;
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public LectureStatus Status { get; set; }
    public bool IsLocked => Status == LectureStatus.Locked;
    public bool IsManuallyUnlocked { get; set; }

    public bool HasMaterial { get; set; }
    public string? MaterialFileName { get; set; }

    public int PassingPercentage { get; set; }
    public int MaxAttempts { get; set; }

    // Условия завершения
    public bool TestRequired { get; set; }
    public bool TestPassed { get; set; }
    public int RequiredQuestions { get; set; }
    public int RequiredQuestionsDone { get; set; }
    public int PendingReviewCount { get; set; }
}

public class StudentTopicViewModel
{
    public int TopicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<LectureStateViewModel> Lectures { get; set; } = new();
}

// Структура дисциплины: Дисциплина → Темы → Лекции
public class StudentDisciplineViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int TotalLectures { get; set; }
    public int CompletedLectures { get; set; }
    public int ProgressPercent { get; set; }
    public List<StudentTopicViewModel> Topics { get; set; } = new();
}

// Контрольный вопрос на странице лекции вместе с ответом студента
public class LectureQuestionViewModel
{
    public int Id { get; set; }
    public int Number { get; set; }
    public string Text { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public bool IsRequired { get; set; }
    public bool RequiresManualReview { get; set; }
    public List<TakeOptionViewModel> Options { get; set; } = new();

    // Ответ студента (если есть)
    public bool HasAnswer { get; set; }
    public string? AnswerText { get; set; }
    public int? SelectedOptionId { get; set; }
    public bool IsCorrect { get; set; }
    public ReviewStatus ReviewStatus { get; set; }

    // Можно ли сейчас отправить (или заново отправить) ответ
    public bool CanAnswer { get; set; }
}

// Страница лекции
public class StudentLecturePageViewModel
{
    public LectureStateViewModel State { get; set; } = new();
    public int DisciplineId { get; set; }
    public string DisciplineName { get; set; } = string.Empty;
    public bool RequireControlQuestions { get; set; }
    public List<LectureQuestionViewModel> Questions { get; set; } = new();
    public int TestAttemptsUsed { get; set; }
    public LectureStateViewModel? Next { get; set; }
}

// Ответ студента на контрольный вопрос (данные формы)
public class LectureAnswerInput
{
    public int LectureId { get; set; }
    public int QuestionId { get; set; }

    [StringLength(2000)]
    public string? AnswerText { get; set; }

    public int? SelectedOptionId { get; set; }
}