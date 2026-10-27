using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ElectronicStudyCabinet.ViewModels;

// Форма создания и изменения лекции
public class LectureFormViewModel
{
    public int Id { get; set; }

    public int TopicId { get; set; }

    // Только для показа на странице
    public string? TopicName { get; set; }
    public int DisciplineId { get; set; }
    public string? DisciplineName { get; set; }
    public string? CurrentMaterialFileName { get; set; }

    [Required(ErrorMessage = "Введите название лекции")]
    [StringLength(200, ErrorMessage = "Название не должно быть длиннее 200 символов")]
    [Display(Name = "Название")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Описание не должно быть длиннее 2000 символов")]
    [Display(Name = "Краткое описание")]
    public string? Description { get; set; }

    [Range(1, 1000, ErrorMessage = "Порядок должен быть числом от 1 до 1000")]
    [Display(Name = "Порядок изучения в теме")]
    public int OrderIndex { get; set; } = 1;

    [Display(Name = "Опубликована (видна студентам)")]
    public bool IsPublished { get; set; }

    [Range(1, 100, ErrorMessage = "Проходной процент должен быть от 1 до 100")]
    [Display(Name = "Проходной процент по тесту")]
    public int PassingPercentage { get; set; } = 70;

    [Range(1, 100, ErrorMessage = "Количество попыток должно быть от 1 до 100")]
    [Display(Name = "Количество попыток")]
    public int MaxAttempts { get; set; } = 3;

    [Display(Name = "Контрольные вопросы обязательны для открытия следующей лекции")]
    public bool RequireControlQuestions { get; set; }

    [Display(Name = "Файл материала (PDF или DOCX)")]
    public IFormFile? MaterialFile { get; set; }
}

// Строка на странице "Лекции"
public class LectureRowViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public bool IsPublished { get; set; }
    public string? MaterialFileName { get; set; }
    public int TopicId { get; set; }
    public string TopicName { get; set; } = string.Empty;
    public int DisciplineId { get; set; }
    public string DisciplineName { get; set; } = string.Empty;
}