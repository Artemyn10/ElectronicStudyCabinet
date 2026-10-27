using ElectronicStudyCabinet.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStudyCabinet.Controllers;

[Authorize(Roles = AppRoles.Student)]
public class StudentController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}