using System.Security.Claims;
using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStudyCabinet.Controllers;

[Authorize(Roles = AppRoles.Teacher)]
public class ReviewsController : Controller
{
    private readonly IReviewService _reviews;

    public ReviewsController(IReviewService reviews)
    {
        _reviews = reviews;
    }

    public async Task<IActionResult> Index()
    {
        var items = await _reviews.GetPendingAsync();
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(int id, bool accept)
    {
        var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (await _reviews.ReviewAsync(id, accept, teacherId))
            TempData["Success"] = accept ? "Ответ принят" : "Ответ отклонён";
        else
            TempData["Warning"] = "Этот ответ уже проверен или не найден";

        return RedirectToAction(nameof(Index));
    }
}