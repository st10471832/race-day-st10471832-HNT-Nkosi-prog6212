using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class ParticipantController : Controller
{
    private readonly ApiClient _api;

    public ParticipantController(ApiClient api)
    {
        _api = api;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var role = HttpContext.Session.GetString("Role");
        if (role == null)
            context.Result = RedirectToAction("Login", "Account");
        else if (role != "Participant")
            context.Result = RedirectToAction("Index", "Home");

        base.OnActionExecuting(context);
    }

    public async Task<IActionResult> MyEvents()
    {
        var result = await _api.GetAsync<List<EnrolmentDto>>("api/participants/me/enrolments");

        ViewBag.Error = TempData["Error"] ?? (result.Success ? null : result.Error);

        return View((result.Data ?? new List<EnrolmentDto>()).OrderBy(e => e.EventDate).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        var result = await _api.DeleteAsync($"api/enrolments/{id}");

        if (!result.Success)
            TempData["Error"] = result.Error;
        else
            TempData["Success"] = "Enrolment cancelled.";

        return RedirectToAction(nameof(MyEvents));
    }

    public async Task<IActionResult> MyResults()
    {
        var result = await _api.GetAsync<List<ResultDto>>("api/participants/me/results");

        if (!result.Success)
            ViewBag.Error = result.Error;

        return View((result.Data ?? new List<ResultDto>()).OrderByDescending(r => r.EventName).ToList());
    }
}