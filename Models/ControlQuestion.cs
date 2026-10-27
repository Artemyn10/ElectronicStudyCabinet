using System.ComponentModel.DataAnnotations;

namespace ElectronicStudyCabinet.Models;

public class ControlQuestion
{
    public int Id { get; set; }

    public int LectureId { get; set; }
    public Lecture Lecture { get; set; } = null!;

    [Required]
    [MaxLength(2000)]
    public string Text { get; set; } = string.Empty;

    public QuestionType QuestionType { get; set; }

    // Правильный ответ для вопросов с кратким текстовым ответом
    [MaxLength(500)]
    public string? CorrectAnswer { get; set; }

    // Обязателен ли вопрос для открытия следующей лекции
    public bool IsRequired { get; set; } = true;

    // Должен ли преподаватель проверить ответ вручную
    public bool RequiresManualReview { get; set; }

    public int OrderIndex { get; set; }

    public List<ControlQuestionOption> Options { get; set; } = new();
    public List<StudentAnswer> StudentAnswers { get; set; } = new();
}