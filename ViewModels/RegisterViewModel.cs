using System.ComponentModel.DataAnnotations;

namespace ElectronicStudyCabinet.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Введите ФИО")]
    [StringLength(200, ErrorMessage = "ФИО не должно быть длиннее 200 символов")]
    [Display(Name = "ФИО")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите логин")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Логин должен содержать от 3 до 50 символов")]
    [Display(Name = "Логин")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите электронную почту")]
    [EmailAddress(ErrorMessage = "Некорректный адрес электронной почты")]
    [Display(Name = "Электронная почта")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите пароль")]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Подтвердите пароль")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Пароли не совпадают")]
    [Display(Name = "Подтверждение пароля")]
    public string ConfirmPassword { get; set; } = string.Empty;
}