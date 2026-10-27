using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Services;
using ElectronicStudyCabinet.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStudyCabinet.Controllers;

[Authorize(Roles = AppRoles.Teacher)]
public class LecturesController : Controller
{
    // Ограничение размера запроса: файл до 20 МБ плюс поля формы
    private const long MaxRequestBytes = 25L * 1024 * 1024;

    private readonly ILectureService _lectures;
    private readonly IFileStorageService _storage;

    public LecturesController(ILectureService lectures, IFileStorageService storage)
    {
        _lectures = lectures;
        _storage = storage;
    }

    // Список всех лекций с поиском по названию
    public async Task<IActionResult> Index(string? search)
    {
        ViewData["Search"] = search;
        var items = await _lectures.GetListAsync(search);
        return View(items);
    }

    // /Lectures/Create?topicId=3
    [HttpGet]
    public async Task<IActionResult> Create(int topicId)
    {
        var model = await _lectures.GetNewAsync(topicId);
        if (model == null)
            return NotFound();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxRequestBytes)]
    public async Task<IActionResult> Create(LectureFormViewModel model)
    {
        ValidateFile(model.MaterialFile);

        if (!ModelState.IsValid)
        {
            await _lectures.FillContextAsync(model);
            return View(model);
        }

        var id = await _lectures.CreateAsync(model);
        if (id == null)
            return NotFound();

        TempData["Success"] = "Данные успешно сохранены";
        return await RedirectToDisciplineAsync(id.Value);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _lectures.GetFormAsync(id);
        if (model == null)
            return NotFound();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxRequestBytes)]
    public async Task<IActionResult> Edit(int id, LectureFormViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        ValidateFile(model.MaterialFile);

        if (!ModelState.IsValid)
        {
            await _lectures.FillContextAsync(model);
            return View(model);
        }

        if (!await _lectures.UpdateAsync(model))
            return NotFound();

        TempData["Success"] = "Данные успешно сохранены";
        return await RedirectToDisciplineAsync(id);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var lecture = await _lectures.GetFormAsync(id);
        if (lecture == null)
            return NotFound();

        var result = await _lectures.DeleteAsync(id);

        if (result == DeleteResult.Deleted)
            TempData["Success"] = "Лекция удалена";
        else if (result == DeleteResult.HasChildren)
            TempData["Error"] = "Нельзя удалить лекцию, у которой есть вопросы, тест или результаты студентов.";

        return RedirectToAction("Details", "Disciplines", new { id = lecture.DisciplineId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMaterial(int id)
    {
        if (!await _lectures.DeleteMaterialAsync(id))
            return NotFound();

        TempData["Success"] = "Файл материала удалён";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(int id, bool up)
    {
        if (!await _lectures.MoveAsync(id, up))
            return NotFound();

        return await RedirectToDisciplineAsync(id);
    }

    // ===== Вспомогательные методы =====

    private void ValidateFile(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return;

        var error = _storage.Validate(file);
        if (error != null)
            ModelState.AddModelError(nameof(LectureFormViewModel.MaterialFile), error);
    }

    // Возвращает на страницу дисциплины, к которой относится лекция
    private async Task<IActionResult> RedirectToDisciplineAsync(int lectureId)
    {
        var lecture = await _lectures.GetFormAsync(lectureId);
        if (lecture == null)
            return RedirectToAction("Index", "Disciplines");

        return RedirectToAction("Details", "Disciplines", new { id = lecture.DisciplineId });
    }
}