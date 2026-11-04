namespace ElectronicStudyCabinet.Models;

// Членство студента в группе
public class GroupMembership
{
    public int Id { get; set; }

    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser Student { get; set; } = null!;

    public int StudentGroupId { get; set; }
    public StudentGroup StudentGroup { get; set; } = null!;

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}