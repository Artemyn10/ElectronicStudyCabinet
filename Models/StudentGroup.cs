using System.ComponentModel.DataAnnotations;

namespace ElectronicStudyCabinet.Models;

public class StudentGroup
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int SpecialtyId { get; set; }
    public Specialty Specialty { get; set; } = null!;

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<GroupMembership> Members { get; set; } = new();
}