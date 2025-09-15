using Microsoft.AspNetCore.Mvc;
using ElfAndSafety.Models;
using ElfAndSafety.Services;

namespace ElfAndSafety.Controllers;

public class UserController : Controller
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public IActionResult Create()
    {
        return PartialView("_CreateUserModal", new User());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(User user)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await _userService.CreateUserAsync(user);
                return Json(new { success = true, message = "User created successfully" });
            }
            catch (DuplicateUserException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return PartialView("_CreateUserModal", user);
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("EmailAddress", ex.Message);
                return PartialView("_CreateUserModal", user);
            }
        }

        return PartialView("_CreateUserModal", user);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        return PartialView("_EditUserModal", user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(User user)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await _userService.UpdateUserAsync(user);
                return Json(new { success = true, message = "User updated successfully" });
            }
            catch (DuplicateUserException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return PartialView("_EditUserModal", user);
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("EmailAddress", ex.Message);
                return PartialView("_EditUserModal", user);
            }
        }

        return PartialView("_EditUserModal", user);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _userService.DeleteUserAsync(id);
        if (success)
        {
            return Json(new { success = true, message = "User deleted successfully" });
        }

        return Json(new { success = false, message = "User not found" });
    }

    [HttpPost]
    public async Task<IActionResult> Restore(int id)
    {
        var success = await _userService.RestoreUserAsync(id);
        if (success)
        {
            return Json(new { success = true, message = "User restored successfully" });
        }

        return Json(new { success = false, message = "User not found" });
    }

    [HttpPost]
    public async Task<IActionResult> PermanentDelete(int id)
    {
        var success = await _userService.PermanentlyDeleteUserAsync(id);
        if (success)
        {
            return Json(new { success = true, message = "User permanently deleted" });
        }

        return Json(new { success = false, message = "User not found or could not be deleted" });
    }
}
