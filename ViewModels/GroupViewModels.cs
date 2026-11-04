using System.ComponentModel.DataAnnotations;

namespace ElectronicStudyCabinet.ViewModels;

public class SimpleOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// ===== Специальности =====

public class SpecialtyFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Введите название специальности")]
    [StringLength(200, ErrorMessage = "Название не должно быть длиннее 200 символов")]
    [Display(Name = "Название специальности")]
    public string Name { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "Код не должен быть длиннее 20 символов")]
    [Display(Name = "Код специальности")]
    public string? Code { get; set; }
}

public class SpecialtyRowViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int GroupsCount { get; set; }
    public int StudentsCount { get; set; }
}

// ===== Группы =====

public class GroupFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Введите название группы")]
    [StringLength(100, ErrorMessage = "Название не должно быть длиннее 100 символов")]
    [Display(Name = "Название группы")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Выберите специальность")]
    [Display(Name = "Специальность")]
    public int? SpecialtyId { get; set; }

    [StringLength(500, ErrorMessage = "Описание не должно быть длиннее 500 символов")]
    [Display(Name = "Описание")]
    public string? Description { get; set; }

    // Список для выпадающего поля (не вводится пользователем)
    public List<SimpleOptionViewModel> Specialties { get; set; } = new();
}

public class GroupRowViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SpecialtyName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int StudentsCount { get; set; }
}

public class GroupsListViewModel
{
    public string? Search { get; set; }
    public int? SpecialtyId { get; set; }
    public List<GroupRowViewModel> Items { get; set; } = new();
    public List<SimpleOptionViewModel> Specialties { get; set; } = new();
}

public class AvailableStudentViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? UserName { get; set; }
}

public class GroupDetailsViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SpecialtyName { get; set; } = string.Empty;
    public string? SpecialtyCode { get; set; }
    public string? Description { get; set; }

    public int AverageProgress { get; set; }

    // Студенты группы (с прогрессом) и студенты, которых ещё можно добавить
    public List<StudentRowViewModel> Members { get; set; } = new();
    public List<AvailableStudentViewModel> AvailableStudents { get; set; } = new();
}