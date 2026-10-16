using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Controllers;

[ApiController]
public class UsersController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public UsersController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Returns the logged-in user's own profile.</summary>
    /// <response code="200">The profile of the logged-in user.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet("api/users/me")]
    [SessionAuthorize]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyProfile()
    {
        var profile = await LoadProfile();
        if (profile == null)
            return AccountMissing();

        return Ok(profile);
    }

    /// <summary>Updates the logged-in user's own profile (name, phone and, for Participants, date of birth).</summary>
    /// <response code="200">The updated profile.</response>
    /// <response code="400">Invalid fields.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPut("api/users/me")]
    [SessionAuthorize]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMyProfile(UpdateProfileRequest request)
    {
        if (CurrentRole == Roles.Organiser)
        {
            var organiser = await _db.Organisers.FindAsync(CurrentUserId);
            if (organiser == null)
                return AccountMissing();

            organiser.FullName = request.FullName.Trim();
            organiser.Phone = request.Phone?.Trim();
        }
        else
        {
            var participant = await _db.Participants.FindAsync(CurrentUserId);
            if (participant == null)
                return AccountMissing();

            if (request.DateOfBirth != null && request.DateOfBirth.Value > DateOnly.FromDateTime(DateTime.Today))
                return BadRequest(new ErrorResponse("Date of birth cannot be in the future."));

            participant.FullName = request.FullName.Trim();
            participant.Phone = request.Phone?.Trim();
            if (request.DateOfBirth != null)
                participant.DateOfBirth = request.DateOfBirth.Value;
        }

        await _db.SaveChangesAsync();
        return Ok(await LoadProfile());
    }

    private async Task<ProfileResponse?> LoadProfile()
    {
        if (CurrentRole == Roles.Organiser)
        {
            var o = await _db.Organisers.AsNoTracking().FirstOrDefaultAsync(x => x.OrganiserID == CurrentUserId);
            return o == null ? null
                : new ProfileResponse(o.OrganiserID, Roles.Organiser, o.FullName, o.Email, o.Phone, null);
        }

        var p = await _db.Participants.AsNoTracking().FirstOrDefaultAsync(x => x.ParticipantID == CurrentUserId);
        return p == null ? null
            : new ProfileResponse(p.ParticipantID, Roles.Participant, p.FullName, p.Email, p.Phone, p.DateOfBirth);
    }

    private IActionResult AccountMissing()
    {
        HttpContext.Session.Clear();
        return Unauthorized(new ErrorResponse("Your account no longer exists. Please log in again."));
    }
}