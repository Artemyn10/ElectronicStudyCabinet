using ElectronicStudyCabinet.Data;
using ElectronicStudyCabinet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStudyCabinet.Controllers;

[Authorize(Roles = AppRoles.Teacher)]
public class AuditController : Controller
{
    private readonly IAuditService _audit;

    public AuditController(IAuditService audit)
    {
        _audit = audit;
    }

    // /Audit?type=ManualUnlock&search=...
    public async Task<IActionResult> Index(string? type, string? search, int page = 1)
    {
        var model = await _audit.GetPageAsync(type, search, page);
        return View(model);
    }
}