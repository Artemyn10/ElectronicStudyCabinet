using System.Security.Claims;
using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStudyCabinet.Controllers;

// Выдача файлов материалов. Файлы лежат вне wwwroot, прямой ссылки на них нет.
[Authorize]
public class MaterialsController : Controller
{
    private readonly ILectureService _lectures;
    private readonly IFileStorageService _storage;
    private readonly ILectureAccessService _access;

    public MaterialsController(
        ILectureService lectures,
        IFileStorageService storage,
        ILectureAccessService access)
    {
        _lectures = lectures;
        _storage = storage;
        _access = access;
    }

    // /Materials/Download/5 открывает PDF в браузере, /Materials/Download/5?download=true скачивает файл
    public async Task<IActionResult> Download(int id, bool download = false)
    {
        if (User.IsInRole(AppRoles.Student))
        {
            // Студент получает файл, только если ему открыта эта лекция
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var access = await _access.CheckAsync(userId, id);

            if (access.Status == LectureAccessStatus.NotFound)
                return NotFound();

            if (!access.IsAllowed)
                return Forbid();
        }
        else if (!User.IsInRole(AppRoles.Teacher))
        {
            return Forbid();
        }

        var info = await _lectures.GetMaterialAsync(id);
        if (info == null)
            return NotFound();

        var stream = _storage.OpenRead(info.StoredFileName);
        if (stream == null)
            return NotFound();

        var contentType = _storage.GetContentType(info.StoredFileName);
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        if (download || contentType != "application/pdf")
            return File(stream, contentType, info.FileName);

        // PDF показываем прямо в браузере
        Response.Headers.ContentDisposition =
            $"inline; filename*=UTF-8''{Uri.EscapeDataString(info.FileName)}";
        return File(stream, contentType);
    }
}