using System.ComponentModel.DataAnnotations;

namespace ElectronicStudyCabinet.ViewModels;

// Страница теста лекции
public class TestPageViewModel
{
    public int LectureId { get; set; }
    public string LectureTitle { get; set; } = string.Empty;
    public string TopicName { get; set; } = string.Empty;
    public int DisciplineId { get; set; }
    public string DisciplineName { get; set; } = string.Empty;

    // Настройки берутся из лекции
    public int PassingPercentage { get; set; }
    public int MaxAttempts { get; set; }

    public bool HasTest { get; set; }
    public bool IsPublished { get; set; }
    public int AttemptsCount { get; set; }

    public List<TestQuestionRowViewModel> Questions { get; set; } = new();
}

public class TestQuestionRowViewModel
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public List<TestOptionRowViewModel> Options { get; set; } = new();
}

public class TestOptionRowViewModel
{
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

// Один вариант ответа в форме
public class TestOptionInput
{
    [StringLength(500, ErrorMessage = "Вариант ответа не должен быть длиннее 500 символов")]
    public string? Text { get; set; }
}

// Форма создания и изменения вопроса теста
public class TestQuestionFormViewModel
{
    public const int OptionSlots = 6;

    public int Id { get; set; }
    public int LectureId { get; set; }

    // Только для показа на странице
    public string? LectureTitle { get; set; }
    public int DisciplineId { get; set; }
    public string? DisciplineName { get; set; }

    [Required(ErrorMessage = "Введите текст вопроса")]
    [StringLength(2000, ErrorMessage = "Текст вопроса не должен быть длиннее 2000 символов")]
    [Display(Name = "Текст вопроса")]
    public string Text { get; set; } = string.Empty;

    [Range(1, 1000, ErrorMessage = "Порядок должен быть числом от 1 до 1000")]
    [Display(Name = "Порядок отображения")]
    public int OrderIndex { get; set; } = 1;

    public List<TestOptionInput> Options { get; set; } = new();

    // Номер поля (0, 1, 2...), в котором отмечен правильный вариант
    public int? CorrectOptionIndex { get; set; }

    // Приводит список вариантов ровно к OptionSlots полям
    public void Normalize()
    {
        if (Options.Count > OptionSlots)
            Options = Options.Take(OptionSlots).ToList();

        while (Options.Count < OptionSlots)
            Options.Add(new TestOptionInput());
    }
}