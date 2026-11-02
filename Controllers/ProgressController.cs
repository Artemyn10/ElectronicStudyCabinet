using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStudyCabinet.Controllers;

[Authorize(Roles = AppRoles.Teacher)]
public class ProgressController : Controller
{
    private readonly ITeacherReportService _reports;

    public ProgressController(ITeacherReportService reports)
    {
        _reports = reports;
    }

    public async Task<IActionResult> Index(int? disciplineId)
    {
        var model = await _reports.GetProgressAsync(disciplineId);
        return View(model);
    }
}