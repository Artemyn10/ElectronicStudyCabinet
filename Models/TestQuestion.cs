using System.ComponentModel.DataAnnotations;

namespace ElectronicStudyCabinet.Models;

public class TestQuestion
{
    public int Id { get; set; }

    public int TestId { get; set; }
    public Test Test { get; set; } = null!;

    [Required]
    [MaxLength(2000)]
    public string Text { get; set; } = string.Empty;

    public int OrderIndex { get; set; }

    public List<AnswerOption> Options { get; set; } = new();
}