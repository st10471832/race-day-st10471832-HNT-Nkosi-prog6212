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

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var result = await _api.GetAsync<ProfileDto>("api/users/me");

        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.Error ?? "Could not load your profile.";
            return RedirectToAction("Index", "Home");
        }

        var p = result.Data;
        return View(new ProfileViewModel
        {
            FullName = p.FullName,
            Email = p.Email,
            Phone = p.Phone,
            DateOfBirth = p.DateOfBirth,
            ProfilePictureUrl = p.ProfilePictureUrl
        });
    }

    [HttpPost]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _api.PutAsync<object>("api/users/me", new
        {
            fullName = model.FullName,
            phone = model.Phone,
            dateOfBirth = model.DateOfBirth?.ToString("yyyy-MM-dd")
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not update your profile.");
            return View(model);
        }

        HttpContext.Session.SetString("FullName", model.FullName);
        TempData["Success"] = "Profile updated.";
        return RedirectToAction(nameof(Profile));
    }
    [HttpPost]
    public async Task<IActionResult> UploadPicture(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Please choose an image first.";
            return RedirectToAction(nameof(Profile));
        }

        var result = await _api.UploadAsync<object>("api/users/me/picture", file);

        if (!result.Success)
            TempData["Error"] = result.Error;
        else
            TempData["Success"] = "Profile picture updated.";

        return RedirectToAction(nameof(Profile));
    }
    public async Task<IActionResult> MyResults()
    {
        var result = await _api.GetAsync<List<ResultDto>>("api/participants/me/results");

        if (!result.Success)
            ViewBag.Error = result.Error;

        return View((result.Data ?? new List<ResultDto>()).OrderByDescending(r => r.EventName).ToList());
    }
}