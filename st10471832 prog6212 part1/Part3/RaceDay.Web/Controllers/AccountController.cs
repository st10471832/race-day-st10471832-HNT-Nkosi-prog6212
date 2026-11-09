using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : Controller
{
    private readonly ApiClient _api;

    public AccountController(ApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _api.PostAsync<LoginDto>("api/auth/login", new
        {
            email = model.Email,
            password = model.Password
        });

        if (!result.Success || result.Data == null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Login failed.");
            return View(model);
        }

        HttpContext.Session.SetInt32("UserId", result.Data.UserId);
        HttpContext.Session.SetString("Role", result.Data.Role);
        HttpContext.Session.SetString("FullName", result.Data.FullName);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var path = model.Role == "Organiser"
            ? "api/auth/register/organiser"
            : "api/auth/register/participant";

        var result = await _api.PostAsync<object>(path, new
        {
            fullName = model.FullName,
            email = model.Email,
            password = model.Password,
            phone = model.Phone,
            dateOfBirth = model.DateOfBirth?.ToString("yyyy-MM-dd")
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Registration failed.");
            return View(model);
        }

        TempData["Success"] = "Account created. Please log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _api.PostAsync<object>("api/auth/logout");
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }
}