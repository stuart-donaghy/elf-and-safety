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

    public async Task<IActionResult> Index(bool? showDeleted = false, string? sortBy = null, string? sortDir = null)
    {
        var users = (await _userService.GetUsersAsync(showDeleted)).AsQueryable();

        // Default ordering by FirstName then Surname
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            users = users.OrderBy(u => u.FirstName).ThenBy(u => u.Surname);
        }
        else
        {
            var dir = (sortDir ?? "asc").ToLowerInvariant();
            bool desc = dir == "desc";

            users = sortBy.ToLowerInvariant() switch
            {
                "firstname" => desc ? users.OrderByDescending(u => u.FirstName) : users.OrderBy(u => u.FirstName),
                "surname" => desc ? users.OrderByDescending(u => u.Surname) : users.OrderBy(u => u.Surname),
                "emailaddress" => desc ? users.OrderByDescending(u => u.EmailAddress) : users.OrderBy(u => u.EmailAddress),
                "username" => desc ? users.OrderByDescending(u => u.Username) : users.OrderBy(u => u.Username),
                "datecreated" => desc ? users.OrderByDescending(u => u.DateCreated) : users.OrderBy(u => u.DateCreated),
                "datelastmodified" => desc ? users.OrderByDescending(u => u.DateLastModified) : users.OrderBy(u => u.DateLastModified),
                _ => users.OrderBy(u => u.FirstName).ThenBy(u => u.Surname)
            };
        }

        ViewBag.ShowDeleted = showDeleted;
        ViewBag.SortBy = sortBy;
        ViewBag.SortDir = sortDir;
        return View(users.AsEnumerable());
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
