using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Services;
using ElectronicStudyCabinet.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStudyCabinet.Controllers;

[Authorize(Roles = AppRoles.Teacher)]
public class DisciplinesController : Controller
{
    private readonly IDisciplineService _disciplines;

    public DisciplinesController(IDisciplineService disciplines)
    {
        _disciplines = disciplines;
    }

    // Список с поиском: /Disciplines?search=...
    public async Task<IActionResult> Index(string? search)
    {
        ViewData["Search"] = search;
        var items = await _disciplines.GetListAsync(search);
        return View(items);
    }

    public async Task<IActionResult> Details(int id)
    {
        var model = await _disciplines.GetDetailsAsync(id);
        if (model == null)
            return NotFound();

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new DisciplineFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DisciplineFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var id = await _disciplines.CreateAsync(model);
        TempData["Success"] = "Данные успешно сохранены";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _disciplines.GetFormAsync(id);
        if (model == null)
            return NotFound();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DisciplineFormViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        if (!await _disciplines.UpdateAsync(model))
            return NotFound();

        TempData["Success"] = "Данные успешно сохранены";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _disciplines.DeleteAsync(id);

        switch (result)
        {
            case DeleteResult.Deleted:
                TempData["Success"] = "Дисциплина удалена";
                break;
            case DeleteResult.HasChildren:
                TempData["Error"] = "Нельзя удалить дисциплину, в которой есть темы. Сначала удалите темы.";
                break;
            default:
                return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }
}