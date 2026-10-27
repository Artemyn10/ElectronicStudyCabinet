using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Services;
using ElectronicStudyCabinet.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStudyCabinet.Controllers;

[Authorize(Roles = AppRoles.Teacher)]
public class ControlQuestionsController : Controller
{
    private readonly IControlQuestionService _questions;

    public ControlQuestionsController(IControlQuestionService questions)
    {
        _questions = questions;
    }

    // /ControlQuestions?lectureId=5
    public async Task<IActionResult> Index(int lectureId)
    {
        var model = await _questions.GetListAsync(lectureId);
        if (model == null)
            return NotFound();

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int lectureId)
    {
        var model = await _questions.GetNewAsync(lectureId);
        if (model == null)
            return NotFound();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ControlQuestionFormViewModel model)
    {
        model.Normalize();
        await ApplyRulesAsync(model);

        if (!ModelState.IsValid)
        {
            await _questions.FillContextAsync(model);
            return View(model);
        }

        if (!await _questions.CreateAsync(model))
            return NotFound();

        TempData["Success"] = "Данные успешно сохранены";
        return RedirectToAction(nameof(Index), new { lectureId = model.LectureId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _questions.GetFormAsync(id);
        if (model == null)
            return NotFound();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ControlQuestionFormViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        model.Normalize();
        await ApplyRulesAsync(model);

        if (!ModelState.IsValid)
        {
            await _questions.FillContextAsync(model);
            return View(model);
        }

        var lectureId = await _questions.UpdateAsync(model);
        if (lectureId == null)
            return NotFound();

        TempData["Success"] = "Данные успешно сохранены";
        return RedirectToAction(nameof(Index), new { lectureId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var question = await _questions.GetFormAsync(id);
        if (question == null)
            return NotFound();

        var result = await _questions.DeleteAsync(id);

        if (result == DeleteResult.Deleted)
            TempData["Success"] = "Вопрос удалён";
        else if (result == DeleteResult.HasChildren)
            TempData["Error"] = "Нельзя удалить вопрос, на который уже есть ответы студентов.";

        return RedirectToAction(nameof(Index), new { lectureId = question.LectureId });
    }

    // Добавляет в ModelState ошибки из бизнес-правил сервиса
    private async Task ApplyRulesAsync(ControlQuestionFormViewModel model)
    {
        foreach (var error in await _questions.ValidateAsync(model))
            ModelState.AddModelError(error.Key, error.Value);
    }
}