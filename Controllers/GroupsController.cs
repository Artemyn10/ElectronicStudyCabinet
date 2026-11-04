using System.Security.Claims;
using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Services;
using ElectronicStudyCabinet.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStudyCabinet.Controllers;

[Authorize(Roles = AppRoles.Teacher)]
public class GroupsController : Controller
{
    private readonly IGroupService _groups;

    public GroupsController(IGroupService groups)
    {
        _groups = groups;
    }

    // Id преподавателя берём из его авторизации
    private string TeacherId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // /Groups?search=...&specialtyId=...
    public async Task<IActionResult> Index(string? search, int? specialtyId)
    {
        var model = await _groups.GetListAsync(search, specialtyId);
        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var model = await _groups.GetDetailsAsync(id);
        if (model == null)
            return NotFound();

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        return View(await _groups.GetNewAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GroupFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await _groups.FillOptionsAsync(model);
            return View(model);
        }

        var (success, error, id) = await _groups.CreateAsync(TeacherId, model);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            await _groups.FillOptionsAsync(model);
            return View(model);
        }

        TempData["Success"] = "Группа создана. Теперь добавьте в неё студентов.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _groups.GetFormAsync(id);
        if (model == null)
            return NotFound();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, GroupFormViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
        {
            await _groups.FillOptionsAsync(model);
            return View(model);
        }

        var (success, error) = await _groups.UpdateAsync(model);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            await _groups.FillOptionsAsync(model);
            return View(model);
        }

        TempData["Success"] = "Данные успешно сохранены";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (result, _) = await _groups.DeleteAsync(TeacherId, id);

        switch (result)
        {
            case DeleteResult.Deleted:
                TempData["Success"] = "Группа удалена";
                return RedirectToAction(nameof(Index));

            case DeleteResult.HasChildren:
                TempData["Error"] = "Нельзя удалить группу, в которой есть студенты. Сначала исключите студентов.";
                return RedirectToAction(nameof(Details), new { id });

            default:
                return NotFound();
        }
    }

    // Добавление выбранных студентов в группу
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMembers(int groupId, List<string>? studentIds)
    {
        if (studentIds == null || studentIds.Count == 0)
        {
            TempData["Warning"] = "Выберите хотя бы одного студента.";
            return RedirectToAction(nameof(Details), new { id = groupId });
        }

        var (added, skipped) = await _groups.AddMembersAsync(TeacherId, groupId, studentIds);

        if (added > 0)
            TempData["Success"] = $"Студентов добавлено в группу: {added}";

        if (skipped > 0)
            TempData["Warning"] = $"Не добавлено студентов: {skipped} (уже состоят в группе или не найдены).";

        return RedirectToAction(nameof(Details), new { id = groupId });
    }

    // Исключение студента из группы
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMember(int groupId, string studentId)
    {
        if (await _groups.RemoveMemberAsync(TeacherId, groupId, studentId))
            TempData["Success"] = "Студент исключён из группы";
        else
            TempData["Warning"] = "Студент не найден в этой группе";

        return RedirectToAction(nameof(Details), new { id = groupId });
    }
}