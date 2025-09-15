using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ElfAndSafety.Models;
using ElfAndSafety.Services;

namespace ElfAndSafety.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IUserService _userService;

    public HomeController(ILogger<HomeController> logger, IUserService userService)
    {
        _logger = logger;
        _userService = userService;
    }

    public async Task<IActionResult> Index(bool? showDeleted = null)
    {
        var users = await _userService.GetUsersAsync(showDeleted);
        ViewBag.ShowDeleted = showDeleted;
        return View(users);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
