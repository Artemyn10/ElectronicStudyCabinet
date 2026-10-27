using System.ComponentModel.DataAnnotations;

namespace ElectronicStudyCabinet.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Введите логин или электронную почту")]
    [Display(Name = "Логин или электронная почта")]
    public string Login { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите пароль")]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Запомнить меня")]
    public bool RememberMe { get; set; }

    // Куда вернуть пользователя после входа
    public string? ReturnUrl { get; set; }
}