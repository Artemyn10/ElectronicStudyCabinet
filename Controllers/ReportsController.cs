using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStudyCabinet.Controllers;

[Authorize(Roles = AppRoles.Teacher)]
public class ReportsController : Controller
{
    private readonly ITeacherReportService _reports;

    public ReportsController(ITeacherReportService reports)
    {
        _reports = reports;
    }

    public async Task<IActionResult> Index()
    {
        var model = await _reports.GetReportsAsync();
        return View(model);
    }
}