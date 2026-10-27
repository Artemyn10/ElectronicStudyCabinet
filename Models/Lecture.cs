using System.ComponentModel.DataAnnotations;
using static System.Net.Mime.MediaTypeNames;

namespace ElectronicStudyCabinet.Models;

public class Lecture
{
    public int Id { get; set; }

    public int TopicId { get; set; }
    public Topic Topic { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    // Файл материала (PDF/DOCX). Хранится вне wwwroot, в папке PrivateUploads.
    [MaxLength(500)]
    public string? MaterialPath { get; set; }

    [MaxLength(260)]
    public string? MaterialFileName { get; set; }

    public int OrderIndex { get; set; }

    public bool IsPublished { get; set; }

    // Настройки теста этой лекции
    [Range(1, 100)]
    public int PassingPercentage { get; set; } = 70;

    [Range(1, 100)]
    public int MaxAttempts { get; set; } = 3;

    // Обязательны ли контрольные вопросы для открытия следующей лекции
    public bool RequireControlQuestions { get; set; }

    public List<ControlQuestion> ControlQuestions { get; set; } = new();
    public Test? Test { get; set; }
    public List<LectureProgress> ProgressRecords { get; set; } = new();
}