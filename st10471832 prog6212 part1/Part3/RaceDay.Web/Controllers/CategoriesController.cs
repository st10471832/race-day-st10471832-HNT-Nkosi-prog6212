using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class CategoriesController : Controller
{
    private readonly ApiClient _api;

    public CategoriesController(ApiClient api)
    {
        _api = api;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var role = HttpContext.Session.GetString("Role");
        if (role == null)
            context.Result = RedirectToAction("Login", "Account");
        else if (role != "Organiser")
            context.Result = RedirectToAction("Index", "Home");

        base.OnActionExecuting(context);
    }

    public async Task<IActionResult> Index(int eventId)
    {
        var ev = await _api.GetAsync<EventDto>($"api/events/{eventId}");
        var cats = await _api.GetAsync<List<CategoryDto>>($"api/events/{eventId}/categories");

        if (!ev.Success)
        {
            TempData["Error"] = ev.Error;
            return RedirectToAction("Events", "Organiser");
        }

        ViewBag.Event = ev.Data;
        ViewBag.Error = TempData["Error"] ?? cats.Error;
        return View(cats.Data ?? new List<CategoryDto>());
    }

    [HttpPost]
    public async Task<IActionResult> Add(int eventId, string categoryName, decimal distanceKm, int maxParticipants, decimal price)
    {
        if (string.IsNullOrWhiteSpace(categoryName) || distanceKm <= 0 || maxParticipants <= 0 || price < 0)
        {
            TempData["Error"] = "Please enter a name, a distance above 0, a limit above 0 and a valid price.";
            return RedirectToAction(nameof(Index), new { eventId });
        }

        var result = await _api.PostAsync<CategoryDto>($"api/events/{eventId}/categories", new
        {
            categoryName,
            distanceKm,
            maxParticipants,
            price
        });

        if (!result.Success)
            TempData["Error"] = result.Error;
        else
            TempData["Success"] = "Category added.";

        return RedirectToAction(nameof(Index), new { eventId });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, int eventId)
    {
        var result = await _api.DeleteAsync($"api/categories/{id}");

        if (!result.Success)
            TempData["Error"] = result.Error;
        else
            TempData["Success"] = "Category deleted.";

        return RedirectToAction(nameof(Index), new { eventId });
    }
}