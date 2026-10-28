namespace ElectronicStudyCabinet.ViewModels;

// Строка во временном списке тестов на странице студента
public class AvailableTestViewModel
{
    public int LectureId { get; set; }
    public string LectureTitle { get; set; } = string.Empty;
    public string TopicName { get; set; } = string.Empty;
    public string DisciplineName { get; set; } = string.Empty;
    public int QuestionsCount { get; set; }
    public int PassingPercentage { get; set; }
    public int MaxAttempts { get; set; }
    public int AttemptsUsed { get; set; }
    public bool IsPassed { get; set; }
}

// Строка в истории попыток
public class AttemptHistoryRowViewModel
{
    public int AttemptId { get; set; }
    public int AttemptNumber { get; set; }
    public DateTime CompletedAt { get; set; }
    public int CorrectAnswers { get; set; }
    public int TotalQuestions { get; set; }
    public double ScorePercentage { get; set; }
    public bool IsPassed { get; set; }
}

// Страница "Обзор теста" перед началом
public class StudentTestOverviewViewModel
{
    public int LectureId { get; set; }
    public string LectureTitle { get; set; } = string.Empty;
    public string DisciplineName { get; set; } = string.Empty;
    public int QuestionsCount { get; set; }
    public int PassingPercentage { get; set; }
    public int MaxAttempts { get; set; }
    public int AttemptsUsed { get; set; }
    public int AttemptsLeft => Math.Max(0, MaxAttempts - AttemptsUsed);
    public bool HasInProgress { get; set; }
    public bool IsPassed { get; set; }
    public bool CanStart { get; set; }
    public List<AttemptHistoryRowViewModel> History { get; set; } = new();
}

// Страница с вопросами теста (признака "правильный" здесь нет намеренно)
public class StudentTestTakeViewModel
{
    public int AttemptId { get; set; }
    public int LectureId { get; set; }
    public string LectureTitle { get; set; } = string.Empty;
    public string DisciplineName { get; set; } = string.Empty;
    public int AttemptNumber { get; set; }
    public int MaxAttempts { get; set; }
    public int PassingPercentage { get; set; }
    public List<TakeQuestionViewModel> Questions { get; set; } = new();
}

public class TakeQuestionViewModel
{
    public int Id { get; set; }
    public int Number { get; set; }
    public string Text { get; set; } = string.Empty;
    public List<TakeOptionViewModel> Options { get; set; } = new();
}

public class TakeOptionViewModel
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
}

// Страница результата попытки
public class StudentTestResultViewModel
{
    public int AttemptId { get; set; }
    public int LectureId { get; set; }
    public string LectureTitle { get; set; } = string.Empty;
    public string DisciplineName { get; set; } = string.Empty;
    public int AttemptNumber { get; set; }
    public int CorrectAnswers { get; set; }
    public int TotalQuestions { get; set; }
    public double ScorePercentage { get; set; }
    public int PassingPercentage { get; set; }
    public bool IsPassed { get; set; }
    public DateTime CompletedAt { get; set; }
    public int MaxAttempts { get; set; }
    public int AttemptsUsed { get; set; }
    public int AttemptsLeft => Math.Max(0, MaxAttempts - AttemptsUsed);
    public bool CanRetry { get; set; }
    public List<AttemptHistoryRowViewModel> History { get; set; } = new();
}