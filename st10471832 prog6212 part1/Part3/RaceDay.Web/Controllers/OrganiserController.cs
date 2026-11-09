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

    private async Task<List<EventDto>> LoadMyEvents()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var result = await _api.GetAsync<List<EventDto>>("api/events");

        if (!result.Success)
            ViewBag.Error = result.Error;

        return (result.Data ?? new List<EventDto>())
            .Where(e => e.OrganiserID == userId)
            .OrderBy(e => e.EventDate)
            .ToList();
    }

    public async Task<IActionResult> Dashboard()
    {
        var model = new DashboardViewModel();

        foreach (var ev in await LoadMyEvents())
        {
            var count = 0;
            var cats = await _api.GetAsync<List<CategoryDto>>($"api/events/{ev.EventID}/categories");

            foreach (var c in cats.Data ?? new List<CategoryDto>())
            {
                var enrolments = await _api.GetAsync<List<EnrolmentDto>>($"api/categories/{c.CategoryID}/enrolments");
                count += enrolments.Data?.Count ?? 0;
            }

            model.Events.Add(new EventStat { Event = ev, Enrolments = count });
        }

        return View(model);
    }

    public async Task<IActionResult> Events()
    {
        return View(await LoadMyEvents());
    }

    [HttpGet]
    public IActionResult CreateEvent() => View(new EventFormViewModel { EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)) });

    [HttpPost]
    public async Task<IActionResult> CreateEvent(EventFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _api.PostAsync<EventDto>("api/events", BuildBody(model));

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not create the event.");
            return View(model);
        }

        TempData["Success"] = "Event created.";
        return RedirectToAction(nameof(Events));
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var result = await _api.GetAsync<EventDto>($"api/events/{id}");

        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.Error ?? "Event not found.";
            return RedirectToAction(nameof(Events));
        }

        var e = result.Data;
        return View(new EventFormViewModel
        {
            EventID = e.EventID,
            EventName = e.EventName,
            Description = e.Description,
            EventDate = e.EventDate,
            Location = e.Location,
            DistanceKm = e.DistanceKm,
            EventType = e.EventType
        });
    }

    [HttpPost]
    public async Task<IActionResult> EditEvent(EventFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _api.PutAsync<EventDto>($"api/events/{model.EventID}", BuildBody(model));

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not update the event.");
            return View(model);
        }

        TempData["Success"] = "Event updated.";
        return RedirectToAction(nameof(Events));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var result = await _api.DeleteAsync($"api/events/{id}");

        if (!result.Success)
            TempData["Error"] = result.Error;
        else
            TempData["Success"] = "Event deleted.";

        return RedirectToAction(nameof(Events));
    }

    private static object BuildBody(EventFormViewModel model) => new
    {
        eventName = model.EventName,
        description = model.Description,
        eventDate = model.EventDate.ToString("yyyy-MM-dd"),
        location = model.Location,
        distanceKm = model.DistanceKm,
        eventType = model.EventType
    };
}