namespace ElectronicStudyCabinet.ViewModels;

// ===== Главная страница панели =====

public class DashboardResultRowViewModel
{
    public string StudentName { get; set; } = string.Empty;
    public string LectureTitle { get; set; } = string.Empty;
    public string DisciplineName { get; set; } = string.Empty;
    public int CorrectAnswers { get; set; }
    public int TotalQuestions { get; set; }
    public double ScorePercentage { get; set; }
    public bool IsPassed { get; set; }
    public DateTime CompletedAt { get; set; }
}

public class UnfinishedStudentRowViewModel
{
    public string StudentName { get; set; } = string.Empty;
    public string DisciplineName { get; set; } = string.Empty;
    public int CompletedLectures { get; set; }
    public int TotalLectures { get; set; }
    public int ProgressPercent { get; set; }
}

public class TeacherDashboardViewModel
{
    public int DisciplinesCount { get; set; }
    public int PublishedDisciplinesCount { get; set; }
    public int LecturesCount { get; set; }
    public int StudentsCount { get; set; }
    public int UnfinishedAttemptsCount { get; set; }
    public int PendingReviewsCount { get; set; }

    public List<DashboardResultRowViewModel> LatestResults { get; set; } = new();
    public List<UnfinishedStudentRowViewModel> UnfinishedStudents { get; set; } = new();
}

// ===== Обзорные страницы разделов =====

public class TopicOverviewRowViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public int DisciplineId { get; set; }
    public string DisciplineName { get; set; } = string.Empty;
    public int LecturesCount { get; set; }
}

public class ControlQuestionOverviewRowViewModel
{
    public int LectureId { get; set; }
    public string LectureTitle { get; set; } = string.Empty;
    public string TopicName { get; set; } = string.Empty;
    public int DisciplineId { get; set; }
    public string DisciplineName { get; set; } = string.Empty;
    public int QuestionsCount { get; set; }
    public int RequiredCount { get; set; }
    public int ManualReviewCount { get; set; }
    public bool RequireControlQuestions { get; set; }
}

public class TestOverviewRowViewModel
{
    public int LectureId { get; set; }
    public string LectureTitle { get; set; } = string.Empty;
    public string TopicName { get; set; } = string.Empty;
    public int DisciplineId { get; set; }
    public string DisciplineName { get; set; } = string.Empty;
    public bool HasTest { get; set; }
    public bool IsPublished { get; set; }
    public int QuestionsCount { get; set; }
    public int AttemptsCount { get; set; }
}