using System.Security.Claims;
using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStudyCabinet.Controllers;

[Authorize(Roles = AppRoles.Teacher)]
public class StudentsController : Controller
{
    private readonly ITeacherReportService _reports;
    private readonly IManualUnlockService _unlock;

    public StudentsController(ITeacherReportService reports, IManualUnlockService unlock)
    {
        _reports = reports;
        _unlock = unlock;
    }

    // Id преподавателя берём из его авторизации
    private string TeacherId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // /Students?search=...&status=inprogress
    public async Task<IActionResult> Index(string? search, string? status)
    {
        var model = await _reports.GetStudentsAsync(search, status);
        return View(model);
    }

    // /Students/Details?id=...
    public async Task<IActionResult> Details(string id)
    {
        var model = await _reports.GetStudentDetailsAsync(id);
        if (model == null)
            return NotFound();

        return View(model);
    }

    // Ручное открытие лекции конкретному студенту
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(string studentId, int lectureId, string? reason)
    {
        var outcome = await _unlock.UnlockAsync(TeacherId, studentId, lectureId, reason);

        switch (outcome)
        {
            case UnlockOutcome.NotFound:
                return NotFound();
            case UnlockOutcome.Ok:
                TempData["Success"] = "Лекция открыта студенту. Действие записано в журнал.";
                break;
            case UnlockOutcome.AlreadyAvailable:
                TempData["Warning"] = "Эта лекция уже доступна студенту.";
                break;
            case UnlockOutcome.NotPublished:
                TempData["Error"] = "Нельзя открыть неопубликованную лекцию или лекцию неопубликованной дисциплины.";
                break;
        }

        return RedirectToAction(nameof(Details), new { id = studentId });
    }

    // Отмена ручного открытия (пока студент не начал лекцию)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeUnlock(string studentId, int lectureId)
    {
        if (await _unlock.RevokeAsync(TeacherId, studentId, lectureId))
            TempData["Success"] = "Ручное открытие отменено. Действие записано в журнал.";
        else
            TempData["Warning"] = "Отменить нельзя: студент уже начал изучать эту лекцию или лекция не была открыта вручную.";

        return RedirectToAction(nameof(Details), new { id = studentId });
    }
}