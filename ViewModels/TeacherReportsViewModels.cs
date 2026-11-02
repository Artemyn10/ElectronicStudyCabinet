using ElectronicStudyCabinet.Models;

namespace ElectronicStudyCabinet.ViewModels;

// ===== Студенты =====

public class StudentRowViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? Email { get; set; }

    public int StartedDisciplines { get; set; }
    public int CompletedLectures { get; set; }
    public int TotalLectures { get; set; }
    public int ProgressPercent { get; set; }

    // Завершённые попытки тестов
    public int AttemptsCount { get; set; }
    public int PassedAttempts { get; set; }
    public double? AverageScore { get; set; }

    public DateTime? LastActivity { get; set; }
}

public class StudentsListViewModel
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public List<StudentRowViewModel> Items { get; set; } = new();
}

public class StudentDetailsViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? Email { get; set; }

    // Те же данные, что студент видит в своём кабинете
    public StudentCabinetViewModel Cabinet { get; set; } = new();
    public List<LockedLectureOptionViewModel> LockedLectures { get; set; } = new();
}

// ===== Результаты =====

public class DisciplineOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ResultRowViewModel
{
    public int AttemptId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string DisciplineName { get; set; } = string.Empty;
    public string LectureTitle { get; set; } = string.Empty;
    public int AttemptNumber { get; set; }
    public int CorrectAnswers { get; set; }
    public int TotalQuestions { get; set; }
    public double ScorePercentage { get; set; }
    public int PassingPercentage { get; set; }
    public bool IsPassed { get; set; }
    public DateTime CompletedAt { get; set; }
}

public class ResultsPageViewModel
{
    public int? DisciplineId { get; set; }
    public string? Student { get; set; }
    public string? Status { get; set; }

    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }

    public List<ResultRowViewModel> Items { get; set; } = new();
    public List<DisciplineOptionViewModel> Disciplines { get; set; } = new();
}

// ===== Прогресс обучения =====

public class ProgressLectureHeaderViewModel
{
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class ProgressStudentRowViewModel
{
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public List<LectureStatus> Statuses { get; set; } = new();
    public int Completed { get; set; }
    public int Total { get; set; }
    public int Percent { get; set; }
}

public class ProgressPageViewModel
{
    public List<DisciplineOptionViewModel> Disciplines { get; set; } = new();
    public int? SelectedDisciplineId { get; set; }
    public string? SelectedName { get; set; }
    public List<ProgressLectureHeaderViewModel> Lectures { get; set; } = new();
    public List<ProgressStudentRowViewModel> Rows { get; set; } = new();
}

// ===== Отчёты =====

public class DisciplineReportRowViewModel
{
    public string Name { get; set; } = string.Empty;
    public int LecturesCount { get; set; }
    public int StudentsStarted { get; set; }
    public int StudentsFinished { get; set; }
    public int AverageProgress { get; set; }
    public int AttemptsCount { get; set; }
    public int PassedAttempts { get; set; }
    public int PassRate { get; set; }
    public double? AverageScore { get; set; }
}

public class ReportsViewModel
{
    public List<DisciplineReportRowViewModel> Disciplines { get; set; } = new();
    public List<StudentRowViewModel> Students { get; set; } = new();
}
// Заблокированная лекция, которую можно открыть вручную
public class LockedLectureOptionViewModel
{
    public int LectureId { get; set; }
    public string DisciplineName { get; set; } = string.Empty;
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
}