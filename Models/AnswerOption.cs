using System.ComponentModel.DataAnnotations;

namespace ElectronicStudyCabinet.Models;

public class AnswerOption
{
    public int Id { get; set; }

    public int TestQuestionId { get; set; }
    public TestQuestion TestQuestion { get; set; } = null!;

    [Required]
    [MaxLength(500)]
    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }

    public int OrderIndex { get; set; }
}