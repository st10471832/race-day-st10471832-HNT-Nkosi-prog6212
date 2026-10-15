using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public string? Role { get; set; }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;

        if (session.GetUserId() == null)
        {
            context.Result = new ObjectResult(new ErrorResponse("You must be logged in to do this."))
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            return;
        }

        if (Role != null && session.GetRole() != Role)
        {
            context.Result = new ObjectResult(new ErrorResponse($"This action is only available to the {Role} role."))
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}