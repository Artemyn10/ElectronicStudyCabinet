namespace ElectronicStudyCabinet.Models;

public class LectureProgress
{
    public int Id { get; set; }

    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser Student { get; set; } = null!;

    public int LectureId { get; set; }
    public Lecture Lecture { get; set; } = null!;

    public LectureStatus Status { get; set; } = LectureStatus.InProgress;

    // Преподаватель вручную открыл эту лекцию студенту
    public bool IsManuallyUnlocked { get; set; }

    public DateTime? UnlockedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}