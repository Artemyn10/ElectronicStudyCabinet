using System.ComponentModel.DataAnnotations;

namespace ElectronicStudyCabinet.Models;

public class AuditLog
{
    public int Id { get; set; }

    // Кто выполнил действие
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}