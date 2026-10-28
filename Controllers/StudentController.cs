using System.Security.Claims;
using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStudyCabinet.Controllers;

[Authorize(Roles = AppRoles.Student)]
public class StudentController : Controller
{
    private readonly IStudentTestService _tests;

    public StudentController(IStudentTestService tests)
    {
        _tests = tests;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var items = await _tests.GetAvailableTestsAsync(userId);
        return View(items);
    }
}