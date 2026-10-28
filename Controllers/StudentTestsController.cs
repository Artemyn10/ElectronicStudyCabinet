using System.Security.Claims;
using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStudyCabinet.Controllers;

[Authorize(Roles = AppRoles.Student)]
public class StudentTestsController : Controller
{
    private readonly IStudentTestService _tests;

    public StudentTestsController(IStudentTestService tests)
    {
        _tests = tests;
    }

    // Id текущего студента берём из его авторизации, а не из запроса
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // /StudentTests?lectureId=5 — обзор теста
    public async Task<IActionResult> Index(int lectureId)
    {
        var response = await _tests.GetOverviewAsync(UserId, lectureId);
        if (response.Outcome != TestOutcome.Ok)
            return HandleFailure(response.Outcome);

        return View(response.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int lectureId)
    {
        var response = await _tests.StartAsync(UserId, lectureId);
        if (response.Outcome != TestOutcome.Ok)
            return HandleFailure(response.Outcome, lectureId);

        return RedirectToAction(nameof(Take), new { attemptId = response.Value });
    }

    // /StudentTests/Take?attemptId=12 — вопросы теста
    public async Task<IActionResult> Take(int attemptId)
    {
        var response = await _tests.GetTakeAsync(UserId, attemptId);

        if (response.Outcome == TestOutcome.AlreadyCompleted)
            return RedirectToAction(nameof(Result), new { attemptId });

        if (response.Outcome != TestOutcome.Ok)
            return HandleFailure(response.Outcome);

        return View(response.Value);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int attemptId, Dictionary<int, int>? answers)
    {
        var response = await _tests.SubmitAsync(UserId, attemptId, answers ?? new Dictionary<int, int>());

        // Повторная отправка: просто показываем готовый результат
        if (response.Outcome == TestOutcome.AlreadyCompleted)
            return RedirectToAction(nameof(Result), new { attemptId });

        if (response.Outcome != TestOutcome.Ok)
            return HandleFailure(response.Outcome);

        return RedirectToAction(nameof(Result), new { attemptId });
    }

    // /StudentTests/Result?attemptId=12 — результат
    public async Task<IActionResult> Result(int attemptId)
    {
        var response = await _tests.GetResultAsync(UserId, attemptId);
        if (response.Outcome != TestOutcome.Ok)
            return HandleFailure(response.Outcome);

        return View(response.Value);
    }

    // Превращает причину отказа в понятное сообщение
    private IActionResult HandleFailure(TestOutcome outcome, int? lectureId = null)
    {
        switch (outcome)
        {
            case TestOutcome.NotFound:
                return NotFound();

            case TestOutcome.Forbidden:
                // Страница "Доступ запрещён"
                return Forbid();

            case TestOutcome.LectureLocked:
                TempData["Error"] = "Доступ к лекции запрещён";
                break;

            case TestOutcome.NoTest:
                TempData["Error"] = "Для этой лекции тест сейчас недоступен";
                break;

            case TestOutcome.AttemptsExhausted:
                TempData["Error"] = "Количество попыток исчерпано";
                if (lectureId != null)
                    return RedirectToAction(nameof(Index), new { lectureId });
                break;

            case TestOutcome.AlreadyPassed:
                TempData["Info"] = "Тест уже успешно пройден";
                if (lectureId != null)
                    return RedirectToAction(nameof(Index), new { lectureId });
                break;
        }

        return RedirectToAction("Index", "Student");
    }
}