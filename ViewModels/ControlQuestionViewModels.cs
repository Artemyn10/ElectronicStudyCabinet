using System.ComponentModel.DataAnnotations;
using ElectronicStudyCabinet.Models;

namespace ElectronicStudyCabinet.ViewModels;

// Один вариант ответа в форме
public class ControlQuestionOptionInput
{
    public int Id { get; set; }

    [StringLength(500, ErrorMessage = "Вариант ответа не должен быть длиннее 500 символов")]
    public string? Text { get; set; }
}

// Форма создания и изменения вопроса
public class ControlQuestionFormViewModel
{
    // Сколько полей для вариантов ответа показываем в форме
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

    [Display(Name = "Тип вопроса")]
    public QuestionType QuestionType { get; set; } = QuestionType.ShortText;

    [StringLength(500, ErrorMessage = "Правильный ответ не должен быть длиннее 500 символов")]
    [Display(Name = "Правильный ответ")]
    public string? CorrectAnswer { get; set; }

    [Display(Name = "Обязательный вопрос (нужен для открытия следующей лекции)")]
    public bool IsRequired { get; set; } = true;

    [Display(Name = "Ответ проверяет преподаватель вручную")]
    public bool RequiresManualReview { get; set; }

    [Range(1, 1000, ErrorMessage = "Порядок должен быть числом от 1 до 1000")]
    [Display(Name = "Порядок отображения")]
    public int OrderIndex { get; set; } = 1;

    public List<ControlQuestionOptionInput> Options { get; set; } = new();

    // Номер поля (0, 1, 2...), в котором отмечен правильный вариант
    public int? CorrectOptionIndex { get; set; }

    // Приводит список вариантов ровно к OptionSlots полям
    public void Normalize()
    {
        if (Options.Count > OptionSlots)
            Options = Options.Take(OptionSlots).ToList();

        while (Options.Count < OptionSlots)
            Options.Add(new ControlQuestionOptionInput());
    }
}

// Строка в списке вопросов
public class ControlQuestionRowViewModel
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public bool IsRequired { get; set; }
    public bool RequiresManualReview { get; set; }
    public int OrderIndex { get; set; }
    public string? CorrectAnswerDisplay { get; set; }
}

// Страница списка вопросов лекции
public class ControlQuestionListViewModel
{
    public int LectureId { get; set; }
    public string LectureTitle { get; set; } = string.Empty;
    public string TopicName { get; set; } = string.Empty;
    public int DisciplineId { get; set; }
    public string DisciplineName { get; set; } = string.Empty;
    public bool RequireControlQuestions { get; set; }
    public List<ControlQuestionRowViewModel> Items { get; set; } = new();
}