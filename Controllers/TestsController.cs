using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Services;
using ElectronicStudyCabinet.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStudyCabinet.Controllers;

[Authorize(Roles = AppRoles.Teacher)]
public class TestsController : Controller
{
    private readonly ITestService _tests;

    public TestsController(ITestService tests)
    {
        _tests = tests;
    }

    // /Tests?lectureId=5
    public async Task<IActionResult> Index(int lectureId)
    {
        var model = await _tests.GetPageAsync(lectureId);
        if (model == null)
            return NotFound();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int lectureId)
    {
        if (!await _tests.CreateAsync(lectureId))
            return NotFound();

        TempData["Success"] = "Тест создан. Добавьте вопросы и опубликуйте тест.";
        return RedirectToAction(nameof(Index), new { lectureId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPublished(int lectureId, bool publish)
    {
        var (success, error) = await _tests.SetPublishedAsync(lectureId, publish);

        if (success)
            TempData["Success"] = publish ? "Тест опубликован" : "Тест снят с публикации";
        else
            TempData["Error"] = error;

        return RedirectToAction(nameof(Index), new { lectureId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int lectureId)
    {
        var result = await _tests.DeleteAsync(lectureId);

        switch (result)
        {
            case DeleteResult.Deleted:
                TempData["Success"] = "Тест удалён";
                break;
            case DeleteResult.HasChildren:
                TempData["Error"] = "Нельзя удалить тест, который студенты уже проходили.";
                break;
            default:
                return NotFound();
        }

        return RedirectToAction(nameof(Index), new { lectureId });
    }

    // ===== Вопросы теста =====

    [HttpGet]
    public async Task<IActionResult> AddQuestion(int lectureId)
    {
        var model = await _tests.GetNewQuestionAsync(lectureId);
        if (model == null)
        {
            TempData["Error"] = "Сначала создайте тест для этой лекции.";
            return RedirectToAction(nameof(Index), new { lectureId });
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddQuestion(TestQuestionFormViewModel model)
    {
        model.Normalize();
        ApplyRules(model);

        if (!ModelState.IsValid)
        {
            await _tests.FillContextAsync(model);
            return View(model);
        }

        if (!await _tests.CreateQuestionAsync(model))
            return NotFound();

        TempData["Success"] = "Данные успешно сохранены";
        return RedirectToAction(nameof(Index), new { lectureId = model.LectureId });
    }

    [HttpGet]
    public async Task<IActionResult> EditQuestion(int id)
    {
        var model = await _tests.GetQuestionFormAsync(id);
        if (model == null)
            return NotFound();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditQuestion(int id, TestQuestionFormViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        model.Normalize();
        ApplyRules(model);

        if (!ModelState.IsValid)
        {
            await _tests.FillContextAsync(model);
            return View(model);
        }

        var lectureId = await _tests.UpdateQuestionAsync(model);
        if (lectureId == null)
            return NotFound();

        TempData["Success"] = "Данные успешно сохранены";
        return RedirectToAction(nameof(Index), new { lectureId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuestion(int id)
    {
        var (result, lectureId) = await _tests.DeleteQuestionAsync(id);

        if (result == DeleteResult.NotFound || lectureId == null)
            return NotFound();

        if (result == DeleteResult.Deleted)
            TempData["Success"] = "Вопрос удалён";
        else
            TempData["Error"] = "Нельзя удалить последний вопрос опубликованного теста. Сначала снимите тест с публикации.";

        return RedirectToAction(nameof(Index), new { lectureId });
    }

    private void ApplyRules(TestQuestionFormViewModel model)
    {
        foreach (var error in _tests.ValidateQuestion(model))
            ModelState.AddModelError(error.Key, error.Value);
    }
}