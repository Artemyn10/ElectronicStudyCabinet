using System.ComponentModel.DataAnnotations;

namespace ElectronicStudyCabinet.Models;

public class ControlQuestionOption
{
    public int Id { get; set; }

    public int ControlQuestionId { get; set; }
    public ControlQuestion ControlQuestion { get; set; } = null!;

    [Required]
    [MaxLength(500)]
    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }

    public int OrderIndex { get; set; }
}