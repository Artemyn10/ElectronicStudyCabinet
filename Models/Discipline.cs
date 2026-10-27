using System.ComponentModel.DataAnnotations;

namespace ElectronicStudyCabinet.Models;

public class Discipline
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public bool IsPublished { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Topic> Topics { get; set; } = new();
}