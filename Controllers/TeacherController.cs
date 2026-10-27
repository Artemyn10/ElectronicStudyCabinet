using ElectronicStudyCabinet.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStudyCabinet.Controllers;

[Authorize(Roles = AppRoles.Teacher)]
public class TeacherController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}