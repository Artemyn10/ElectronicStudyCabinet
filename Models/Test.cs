namespace ElectronicStudyCabinet.Models;

public class Test
{
    public int Id { get; set; }

    // У одной лекции не больше одного теста
    public int LectureId { get; set; }
    public Lecture Lecture { get; set; } = null!;

    public bool IsPublished { get; set; }

    public List<TestQuestion> Questions { get; set; } = new();
    public List<TestAttempt> Attempts { get; set; } = new();
}