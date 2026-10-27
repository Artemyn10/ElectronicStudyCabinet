using System.ComponentModel.DataAnnotations;

namespace ElectronicStudyCabinet.ViewModels;

// Форма создания и изменения дисциплины
public class DisciplineFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Введите название дисциплины")]
    [StringLength(200, ErrorMessage = "Название не должно быть длиннее 200 символов")]
    [Display(Name = "Название")]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Описание не должно быть длиннее 2000 символов")]
    [Display(Name = "Описание")]
    public string? Description { get; set; }

    [Display(Name = "Опубликована (видна студентам)")]
    public bool IsPublished { get; set; }
}

// Строка в списке дисциплин
public class DisciplineListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TopicsCount { get; set; }
    public int LecturesCount { get; set; }
}

// Страница дисциплины с её темами
public class DisciplineDetailsViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<TopicListItemViewModel> Topics { get; set; } = new();
}

// Строка в списке тем
public class TopicListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderIndex { get; set; }
    public int LecturesCount { get; set; }
}