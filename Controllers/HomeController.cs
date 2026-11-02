using System.Diagnostics;
using ElectronicStudyCabinet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicStudyCabinet.Controllers;

// Главная страница и страницы ошибок открыты всем
[AllowAnonymous]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }

    // Страница для кодов ошибок 400, 404, 429 и других
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult HttpStatus(int code)
    {
        ViewBag.Code = code;
        ViewBag.Message = code switch
        {
            400 => "Некорректный запрос. Обновите страницу и повторите действие.",
            403 => "Доступ запрещён.",
            404 => "Страница не найдена.",
            429 => "Слишком много запросов. Подождите минуту и повторите попытку.",
            _ => "Произошла ошибка. Попробуйте ещё раз позже."
        };

        return View();
    }
}