using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Auth;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Controllers;

public abstract class RaceDayControllerBase : ControllerBase
{
    protected int CurrentUserId => HttpContext.Session.GetUserId() ?? 0;

    protected string? CurrentRole => HttpContext.Session.GetRole();

    protected IActionResult Forbidden(string message) =>
        StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse(message));
}