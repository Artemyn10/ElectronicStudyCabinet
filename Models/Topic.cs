using System.ComponentModel.DataAnnotations;

namespace ElectronicStudyCabinet.Models;

public class Topic
{
    public int Id { get; set; }

    public int DisciplineId { get; set; }
    public Discipline Discipline { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public int OrderIndex { get; set; }

    public List<Lecture> Lectures { get; set; } = new();
}