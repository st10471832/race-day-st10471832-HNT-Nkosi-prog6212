using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class EventsController : Controller
{
    private readonly ApiClient _api;

    public EventsController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Details(int id)
    {
        var ev = await _api.GetAsync<EventDto>($"api/events/{id}");

        if (!ev.Success || ev.Data == null)
        {
            TempData["Error"] = ev.Error ?? "Event not found.";
            return RedirectToAction("Index", "Home");
        }

        var cats = await _api.GetAsync<List<CategoryDto>>($"api/events/{id}/categories");

        ViewBag.Event = ev.Data;
        ViewBag.Error = TempData["Error"] ?? cats.Error;
        return View(cats.Data ?? new List<CategoryDto>());
    }

    [HttpPost]
    public async Task<IActionResult> Enrol(int categoryId, int eventId)
    {
        var role = HttpContext.Session.GetString("Role");

        if (role == null)
            return RedirectToAction("Login", "Account");

        if (role != "Participant")
        {
            TempData["Error"] = "Only participants can enrol in events.";
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        var result = await _api.PostAsync<object>($"api/categories/{categoryId}/enrol");

        if (!result.Success)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        TempData["Success"] = "You are enrolled!";
        return RedirectToAction("MyEvents", "Participant");
    }
}