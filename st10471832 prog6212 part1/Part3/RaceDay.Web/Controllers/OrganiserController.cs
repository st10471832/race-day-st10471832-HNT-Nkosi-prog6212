using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class OrganiserController : Controller
{
    private readonly ApiClient _api;

    public OrganiserController(ApiClient api)
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

    public async Task<IActionResult> Events()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var result = await _api.GetAsync<List<EventDto>>("api/events");

        if (!result.Success)
            ViewBag.Error = result.Error;

        var mine = (result.Data ?? new List<EventDto>())
            .Where(e => e.OrganiserID == userId)
            .OrderBy(e => e.EventDate)
            .ToList();

        return View(mine);
    }

    [HttpGet]
    public IActionResult CreateEvent() => View(new EventFormViewModel { EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)) });

    [HttpPost]
    public async Task<IActionResult> CreateEvent(EventFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _api.PostAsync<EventDto>("api/events", new
        {
            eventName = model.EventName,
            description = model.Description,
            eventDate = model.EventDate.ToString("yyyy-MM-dd"),
            location = model.Location,
            distanceKm = model.DistanceKm,
            eventType = model.EventType
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not create the event.");
            return View(model);
        }

        TempData["Success"] = "Event created.";
        return RedirectToAction(nameof(Events));
    }
}