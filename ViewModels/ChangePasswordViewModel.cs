using System.ComponentModel.DataAnnotations;

namespace ElectronicStudyCabinet.ViewModels;

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Введите текущий пароль")]
    [DataType(DataType.Password)]
    [Display(Name = "Текущий пароль")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите новый пароль")]
    [DataType(DataType.Password)]
    [Display(Name = "Новый пароль")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Подтвердите новый пароль")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Пароли не совпадают")]
    [Display(Name = "Подтверждение нового пароля")]
    public string ConfirmPassword { get; set; } = string.Empty;
}