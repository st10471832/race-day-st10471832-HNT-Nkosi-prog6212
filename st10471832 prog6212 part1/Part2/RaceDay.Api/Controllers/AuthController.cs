using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
public class AuthController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public AuthController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Registers a new Organiser account.</summary>
    /// <response code="201">Account created. Returns the new profile.</response>
    /// <response code="400">Missing or invalid fields.</response>
    /// <response code="409">The email address is already registered.</response>
    [HttpPost("api/auth/register/organiser")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterOrganiser(RegisterOrganiserRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await EmailInUse(email))
            return Conflict(new ErrorResponse("An account with this email already exists."));

        var organiser = new Organiser
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = PasswordHelper.Hash(request.Password),
            Phone = request.Phone?.Trim()
        };

        _db.Organisers.Add(organiser);
        await _db.SaveChangesAsync();

        var profile = new ProfileResponse(organiser.OrganiserID, Roles.Organiser,
            organiser.FullName, organiser.Email, organiser.Phone, null);
        return Created("/api/users/me", profile);
    }

    /// <summary>Registers a new Participant account.</summary>
    /// <response code="201">Account created. Returns the new profile.</response>
    /// <response code="400">Missing or invalid fields.</response>
    /// <response code="409">The email address is already registered.</response>
    [HttpPost("api/auth/register/participant")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterParticipant(RegisterParticipantRequest request)
    {
        if (request.DateOfBirth!.Value > DateOnly.FromDateTime(DateTime.Today))
            return BadRequest(new ErrorResponse("Date of birth cannot be in the future."));

        var email = request.Email.Trim().ToLowerInvariant();
        if (await EmailInUse(email))
            return Conflict(new ErrorResponse("An account with this email already exists."));

        var participant = new Participant
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = PasswordHelper.Hash(request.Password),
            Phone = request.Phone?.Trim(),
            DateOfBirth = request.DateOfBirth.Value
        };

        _db.Participants.Add(participant);
        await _db.SaveChangesAsync();

        var profile = new ProfileResponse(participant.ParticipantID, Roles.Participant,
            participant.FullName, participant.Email, participant.Phone, participant.DateOfBirth);
        return Created("/api/users/me", profile);
    }

    /// <summary>Logs in an Organiser or Participant and starts a session.</summary>
    /// <response code="200">Logged in. The session now stores your user ID and role.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("api/auth/login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var organiser = await _db.Organisers.FirstOrDefaultAsync(o => o.Email == email);
        if (organiser != null && PasswordHelper.Verify(organiser.PasswordHash, request.Password))
        {
            HttpContext.Session.SignIn(organiser.OrganiserID, Roles.Organiser);
            return Ok(new LoginResponse(organiser.OrganiserID, Roles.Organiser, organiser.FullName, "Logged in as Organiser."));
        }

        var participant = await _db.Participants.FirstOrDefaultAsync(p => p.Email == email);
        if (participant != null && PasswordHelper.Verify(participant.PasswordHash, request.Password))
        {
            HttpContext.Session.SignIn(participant.ParticipantID, Roles.Participant);
            return Ok(new LoginResponse(participant.ParticipantID, Roles.Participant, participant.FullName, "Logged in as Participant."));
        }

        return Unauthorized(new ErrorResponse("Invalid email or password."));
    }

    /// <summary>Logs out and clears the session.</summary>
    /// <response code="204">Logged out.</response>
    [HttpPost("api/auth/logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return NoContent();
    }

    private async Task<bool> EmailInUse(string email) =>
        await _db.Organisers.AnyAsync(o => o.Email == email) ||
        await _db.Participants.AnyAsync(p => p.Email == email);
}