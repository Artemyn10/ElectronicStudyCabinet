using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Services;
using ElectronicStudyCabinet.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStudyCabinet.Controllers;

[Authorize(Roles = AppRoles.Teacher)]
public class SpecialtiesController : Controller
{
    private readonly ISpecialtyService _specialties;

    public SpecialtiesController(ISpecialtyService specialties)
    {
        _specialties = specialties;
    }

    public async Task<IActionResult> Index(string? search)
    {
        ViewData["Search"] = search;
        var items = await _specialties.GetListAsync(search);
        return View(items);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new SpecialtyFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SpecialtyFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var (success, error) = await _specialties.CreateAsync(model);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            return View(model);
        }

        TempData["Success"] = "Данные успешно сохранены";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _specialties.GetFormAsync(id);
        if (model == null)
            return NotFound();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SpecialtyFormViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        var (success, error) = await _specialties.UpdateAsync(model);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            return View(model);
        }

        TempData["Success"] = "Данные успешно сохранены";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _specialties.DeleteAsync(id);

        switch (result)
        {
            case DeleteResult.Deleted:
                TempData["Success"] = "Специальность удалена";
                break;
            case DeleteResult.HasChildren:
                TempData["Error"] = "Нельзя удалить специальность, у которой есть группы. Сначала удалите или измените группы.";
                break;
            default:
                return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }
}