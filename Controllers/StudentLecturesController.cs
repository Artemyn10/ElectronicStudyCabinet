using System.Security.Claims;
using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Services;
using ElectronicStudyCabinet.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStudyCabinet.Controllers;

[Authorize(Roles = AppRoles.Student)]
public class StudentLecturesController : Controller
{
    private readonly IStudentLectureService _lectures;
    private readonly ILectureAccessService _access;
    private readonly ILectureProgressService _progress;
    private readonly IControlAnswerService _answers;

    public StudentLecturesController(
        IStudentLectureService lectures,
        ILectureAccessService access,
        ILectureProgressService progress,
        IControlAnswerService answers)
    {
        _lectures = lectures;
        _access = access;
        _progress = progress;
        _answers = answers;
    }

    // Id студента берём из его авторизации, а не из запроса
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // /StudentLectures/Discipline/3 — структура дисциплины
    public async Task<IActionResult> Discipline(int id)
    {
        var model = await _lectures.GetDisciplineAsync(UserId, id);
        if (model == null)
            return NotFound();

        return View(model);
    }

    // /StudentLectures/Lecture/5 — страница лекции
    public async Task<IActionResult> Lecture(int id)
    {
        // Главная проверка: можно ли этому студенту открыть эту лекцию
        var access = await _access.CheckAsync(UserId, id);

        if (access.Status == LectureAccessStatus.NotFound)
            return NotFound();

        if (!access.IsAllowed)
        {
            TempData["Error"] = access.Message ?? "Доступ к лекции запрещён";
            return RedirectToAction("Index", "Student");
        }

        // Фиксируем, что лекция открыта, и пересчитываем прогресс
        await _progress.OpenAsync(UserId, id);

        var model = await _lectures.GetLecturePageAsync(UserId, id);
        if (model == null)
            return NotFound();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Answer(LectureAnswerInput input)
    {
        var outcome = await _answers.SubmitAsync(UserId, input);

        switch (outcome)
        {
            case AnswerOutcome.NotFound:
                return NotFound();

            case AnswerOutcome.Locked:
                TempData["Error"] = "Доступ к лекции запрещён";
                return RedirectToAction("Index", "Student");

            case AnswerOutcome.Correct:
                TempData["Success"] = "Ответ верный";
                break;
            case AnswerOutcome.Incorrect:
                TempData["Warning"] = "Ответ неверный. Попробуйте ещё раз.";
                break;
            case AnswerOutcome.SentForReview:
                TempData["Info"] = "Ответ отправлен на проверку преподавателю";
                break;
            case AnswerOutcome.AlreadyAccepted:
                TempData["Info"] = "Ответ на этот вопрос уже принят";
                break;
            case AnswerOutcome.AlreadyPending:
            case AnswerOutcome.Duplicate:
                TempData["Info"] = "Ответ уже отправлен и ожидает проверки";
                break;
            default:
                TempData["Error"] = "Введите корректный ответ";
                break;
        }

        return RedirectToAction(nameof(Lecture), new { id = input.LectureId });
    }
}