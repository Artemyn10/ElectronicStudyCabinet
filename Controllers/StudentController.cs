using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Models;
using ElectronicStudyCabinet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStudyCabinet.Controllers;

[Authorize(Roles = AppRoles.Student)]
public class StudentController : Controller
{
    private readonly IStudentCabinetService _cabinet;
    private readonly UserManager<ApplicationUser> _userManager;

    public StudentController(
        IStudentCabinetService cabinet,
        UserManager<ApplicationUser> userManager)
    {
        _cabinet = cabinet;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        // Текущего студента определяем по его авторизации
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var model = await _cabinet.GetCabinetAsync(user.Id, user.FullName);
        return View(model);
    }
}