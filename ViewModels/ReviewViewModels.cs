namespace ElectronicStudyCabinet.ViewModels;

public class PendingReviewRowViewModel
{
    public int AnswerId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string DisciplineName { get; set; } = string.Empty;
    public string LectureTitle { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public string? ReferenceAnswer { get; set; }
    public string? AnswerText { get; set; }
    public DateTime SubmittedAt { get; set; }
}