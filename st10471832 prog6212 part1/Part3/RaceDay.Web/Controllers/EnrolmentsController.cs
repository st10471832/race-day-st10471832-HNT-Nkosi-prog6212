using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class EnrolmentsController : Controller
{
    private readonly ApiClient _api;

    public EnrolmentsController(ApiClient api)
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
        if (!ev.Success || ev.Data == null)
        {
            TempData["Error"] = ev.Error ?? "Event not found.";
            return RedirectToAction("Events", "Organiser");
        }

        var cats = await _api.GetAsync<List<CategoryDto>>($"api/events/{eventId}/categories");

        var enrolments = new List<EnrolmentDto>();
        var results = new Dictionary<int, ResultDto>();
        string? error = TempData["Error"] as string;

        foreach (var c in cats.Data ?? new List<CategoryDto>())
        {
            var e = await _api.GetAsync<List<EnrolmentDto>>($"api/categories/{c.CategoryID}/enrolments");
            if (e.Success && e.Data != null)
                enrolments.AddRange(e.Data);
            else if (!e.Success)
                error ??= e.Error;

            var r = await _api.GetAsync<List<ResultDto>>($"api/categories/{c.CategoryID}/results");
            if (r.Success && r.Data != null)
                foreach (var item in r.Data)
                    results[item.EnrolmentID] = item;
        }

        ViewBag.Event = ev.Data;
        ViewBag.Results = results;
        ViewBag.Error = error;

        return View(enrolments.OrderBy(x => x.CategoryName).ThenBy(x => x.ParticipantName).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> SaveResult(int enrolmentId, int eventId, string? finishTime, int? position, string status)
    {
        TimeSpan? time = null;

        if (!string.IsNullOrWhiteSpace(finishTime))
        {
            if (!TimeSpan.TryParse(finishTime, out var parsed))
            {
                TempData["Error"] = "Finish time must look like 01:25:30 (hours:minutes:seconds).";
                return RedirectToAction(nameof(Index), new { eventId });
            }
            time = parsed;
        }

        var result = await _api.PostAsync<ResultDto>($"api/enrolments/{enrolmentId}/result", new
        {
            finishTime = time?.ToString(@"hh\:mm\:ss"),
            position,
            status
        });

        if (!result.Success)
            TempData["Error"] = result.Error;
        else
            TempData["Success"] = "Result saved.";

        return RedirectToAction(nameof(Index), new { eventId });
    }
}