using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Models;
using ElectronicStudyCabinet.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStudyCabinet.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // ===== Регистрация =====

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToHome();

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = new ApplicationUser
        {
            UserName = model.UserName.Trim(),
            Email = model.Email.Trim(),
            FullName = model.FullName.Trim()
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        // Через регистрацию всегда назначается роль Student
        var roleResult = await _userManager.AddToRoleAsync(user, AppRoles.Student);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            ModelState.AddModelError(string.Empty, "Не удалось завершить регистрацию. Попробуйте ещё раз.");
            return View(model);
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        TempData["Success"] = "Регистрация прошла успешно. Добро пожаловать!";
        return RedirectToAction("Index", "Student");
    }

    // ===== Вход =====

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToHome();

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var login = model.Login.Trim();

        // Ищем пользователя по логину, а если не нашли, то по электронной почте
        var user = await _userManager.FindByNameAsync(login)
                   ?? await _userManager.FindByEmailAsync(login);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Неверный логин или пароль");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            // Возвращаем на прежнюю страницу, только если ссылка локальная
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return await RedirectByRoleAsync(user);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty,
                "Слишком много неудачных попыток входа. Повторите через 10 минут.");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Неверный логин или пароль");
        }

        return View(model);
    }

    // ===== Выход =====

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        TempData["Success"] = "Вы вышли из системы";
        return RedirectToAction("Index", "Home");
    }

    // ===== Доступ запрещён =====

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    // ===== Вспомогательные методы =====

    private IActionResult RedirectToHome()
    {
        return User.IsInRole(AppRoles.Teacher)
            ? RedirectToAction("Index", "Teacher")
            : RedirectToAction("Index", "Student");
    }

    private async Task<IActionResult> RedirectByRoleAsync(ApplicationUser user)
    {
        return await _userManager.IsInRoleAsync(user, AppRoles.Teacher)
            ? RedirectToAction("Index", "Teacher")
            : RedirectToAction("Index", "Student");
    }
}