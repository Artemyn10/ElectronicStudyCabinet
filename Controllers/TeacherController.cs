using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStudyCabinet.Controllers;

[Authorize(Roles = AppRoles.Teacher)]
public class TeacherController : Controller
{
    private readonly ITeacherDashboardService _dashboard;

    public TeacherController(ITeacherDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    public async Task<IActionResult> Index()
    {
        var model = await _dashboard.GetAsync();
        return View(model);
    }
}