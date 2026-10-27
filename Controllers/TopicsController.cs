using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Services;
using ElectronicStudyCabinet.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStudyCabinet.Controllers;

[Authorize(Roles = AppRoles.Teacher)]
public class TopicsController : Controller
{
    private readonly ITopicService _topics;

    public TopicsController(ITopicService topics)
    {
        _topics = topics;
    }

    // /Topics/Create?disciplineId=5
    [HttpGet]
    public async Task<IActionResult> Create(int disciplineId)
    {
        var model = await _topics.GetNewAsync(disciplineId);
        if (model == null)
            return NotFound();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TopicFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.DisciplineName = await _topics.GetDisciplineNameAsync(model.DisciplineId);
            return View(model);
        }

        if (!await _topics.CreateAsync(model))
            return NotFound();

        TempData["Success"] = "Данные успешно сохранены";
        return RedirectToAction("Details", "Disciplines", new { id = model.DisciplineId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _topics.GetFormAsync(id);
        if (model == null)
            return NotFound();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TopicFormViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
        {
            model.DisciplineName = await _topics.GetDisciplineNameAsync(model.DisciplineId);
            return View(model);
        }

        if (!await _topics.UpdateAsync(model))
            return NotFound();

        TempData["Success"] = "Данные успешно сохранены";
        return RedirectToAction("Details", "Disciplines", new { id = model.DisciplineId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var topic = await _topics.GetFormAsync(id);
        if (topic == null)
            return NotFound();

        var result = await _topics.DeleteAsync(id);

        if (result == DeleteResult.Deleted)
            TempData["Success"] = "Тема удалена";
        else if (result == DeleteResult.HasChildren)
            TempData["Error"] = "Нельзя удалить тему, в которой есть лекции. Сначала удалите лекции.";

        return RedirectToAction("Details", "Disciplines", new { id = topic.DisciplineId });
    }
}