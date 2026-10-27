using System.ComponentModel.DataAnnotations;

namespace ElectronicStudyCabinet.Models;

public class StudentAnswer
{
    public int Id { get; set; }

    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser Student { get; set; } = null!;

    public int ControlQuestionId { get; set; }
    public ControlQuestion ControlQuestion { get; set; } = null!;

    // Ответ студента (для текстовых вопросов)
    [MaxLength(2000)]
    public string? AnswerText { get; set; }

    // Выбранный вариант (для вопросов с выбором)
    public int? SelectedOptionId { get; set; }

    public bool IsCorrect { get; set; }

    public ReviewStatus ReviewStatus { get; set; } = ReviewStatus.AutoChecked;

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }
}