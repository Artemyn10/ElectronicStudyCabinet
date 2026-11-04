namespace ElectronicStudyCabinet.ViewModels;

// Лекция в кабинете: статус + сведения о тесте
public class CabinetLectureViewModel
{
    public LectureStateViewModel State { get; set; } = new();

    // Использовано попыток (включая незавершённую)
    public int AttemptsUsed { get; set; }

    // Лучший результат среди завершённых попыток
    public double? BestScore { get; set; }

    // Дата последней завершённой попытки
    public DateTime? LastAttemptAt { get; set; }
}

public class CabinetTopicViewModel
{
    public int TopicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<CabinetLectureViewModel> Lectures { get; set; } = new();
}

public class CabinetDisciplineViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int TotalLectures { get; set; }
    public int CompletedLectures { get; set; }
    public int ProgressPercent { get; set; }

    // Лекция, с которой стоит продолжить обучение
    public int? ContinueLectureId { get; set; }
    public int ContinueLectureNumber { get; set; }
    public string? ContinueLectureTitle { get; set; }

    public List<CabinetTopicViewModel> Topics { get; set; } = new();
}

// Строка в таблице "Результаты тестирования"
public class CabinetResultRowViewModel
{
    public int LectureId { get; set; }
    public string LectureTitle { get; set; } = string.Empty;
    public string DisciplineName { get; set; } = string.Empty;
    public int PassingPercentage { get; set; }
    public int MaxAttempts { get; set; }
    public int AttemptsUsed { get; set; }
    public double? BestScore { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public bool IsPassed { get; set; }
    public bool AttemptsExhausted => !IsPassed && AttemptsUsed >= MaxAttempts;
}

public class StudentCabinetViewModel
{
    public string FullName { get; set; } = string.Empty;

    public int DisciplinesCount { get; set; }
    public int TotalLectures { get; set; }
    public int CompletedLectures { get; set; }
    public int ProgressPercent { get; set; }

    public List<CabinetDisciplineViewModel> Disciplines { get; set; } = new();
    public List<CabinetResultRowViewModel> Results { get; set; } = new();
    public string? GroupName { get; set; }
    public string? SpecialtyName { get; set; }
}