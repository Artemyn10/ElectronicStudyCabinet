using System.ComponentModel.DataAnnotations;

namespace ElectronicStudyCabinet.ViewModels;

public class TopicFormViewModel
{
    public int Id { get; set; }

    public int DisciplineId { get; set; }

    // Только для показа на странице (не вводится пользователем)
    public string? DisciplineName { get; set; }

    [Required(ErrorMessage = "Введите название темы")]
    [StringLength(200, ErrorMessage = "Название не должно быть длиннее 200 символов")]
    [Display(Name = "Название")]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Описание не должно быть длиннее 2000 символов")]
    [Display(Name = "Описание")]
    public string? Description { get; set; }

    [Range(1, 1000, ErrorMessage = "Порядок должен быть числом от 1 до 1000")]
    [Display(Name = "Порядок отображения")]
    public int OrderIndex { get; set; } = 1;
}