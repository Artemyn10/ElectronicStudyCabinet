using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStudyCabinet.Controllers;

[Authorize(Roles = AppRoles.Teacher)]
public class ResultsController : Controller
{
    private readonly ITeacherReportService _reports;

    public ResultsController(ITeacherReportService reports)
    {
        _reports = reports;
    }

    public async Task<IActionResult> Index(int? disciplineId, string? student, string? status, int page = 1)
    {
        var model = await _reports.GetResultsAsync(disciplineId, student, status, page);
        return View(model);
    }
}