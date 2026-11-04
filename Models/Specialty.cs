using System.ComponentModel.DataAnnotations;

namespace ElectronicStudyCabinet.Models;

public class Specialty
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    // Код специальности (необязательно), например 09.02.07
    [MaxLength(20)]
    public string? Code { get; set; }

    public List<StudentGroup> Groups { get; set; } = new();
}