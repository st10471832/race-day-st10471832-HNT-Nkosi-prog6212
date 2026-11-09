using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApiClient _api;

    public HomeController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(string? search, string? type, string? location)
    {
        var result = await _api.GetAsync<List<EventDto>>("api/events?upcomingOnly=true");

        var events = result.Data ?? new List<EventDto>();

        if (!result.Success)
            ViewBag.Error = result.Error;

        if (!string.IsNullOrWhiteSpace(search))
            events = events.Where(e => e.EventName.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrWhiteSpace(type))
            events = events.Where(e => e.EventType.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrWhiteSpace(location))
            events = events.Where(e => e.Location.Contains(location, StringComparison.OrdinalIgnoreCase)).ToList();

        ViewBag.Search = search;
        ViewBag.Type = type;
        ViewBag.Location = location;

        return View(events.OrderBy(e => e.EventDate).ToList());
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}