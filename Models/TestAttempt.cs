namespace ElectronicStudyCabinet.Models;

public class TestAttempt
{
    public int Id { get; set; }

    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser Student { get; set; } = null!;

    public int TestId { get; set; }
    public Test Test { get; set; } = null!;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public int CorrectAnswers { get; set; }
    public int TotalQuestions { get; set; }
    public double ScorePercentage { get; set; }
    public bool IsPassed { get; set; }

    // Номер попытки студента по этому тесту: 1, 2, 3...
    public int AttemptNumber { get; set; }
}