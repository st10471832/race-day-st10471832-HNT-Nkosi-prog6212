using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
public class EnrolmentsController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Enrols the logged-in Participant into a race category.</summary>
    /// <param name="id">The category ID.</param>
    /// <response code="201">Enrolled. The record links the Participant, the event and the category.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only Participants can enrol.</response>
    /// <response code="404">No category with that ID.</response>
    /// <response code="409">Already enrolled in this category, or the category is full.</response>
    [HttpPost("api/categories/{id:int}/enrol")]
    [SessionAuthorize(Role = Roles.Participant)]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
        if (category == null)
            return NotFound(new ErrorResponse("Category not found."));

        var participant = await _db.Participants.FindAsync(CurrentUserId);
        if (participant == null)
            return Unauthorized(new ErrorResponse("Your account no longer exists. Please log in again."));

        if (await _db.Enrolments.AnyAsync(e => e.CategoryID == id && e.ParticipantID == CurrentUserId))
            return Conflict(new ErrorResponse("You are already enrolled in this category."));

        var enrolled = await _db.Enrolments.CountAsync(e => e.CategoryID == id);
        if (enrolled >= category.MaxParticipants)
            return Conflict(new ErrorResponse("This category is full."));

        var enrolment = new Enrolment
        {
            Category = category,
            Participant = participant,
            Status = "Confirmed"
        };

        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        return Created("/api/participants/me/enrolments", enrolment.ToResponse());
    }

    /// <summary>Lists the logged-in Participant's own enrolments.</summary>
    /// <response code="200">Your enrolments.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only Participants have enrolments.</response>
    [HttpGet("api/participants/me/enrolments")]
    [SessionAuthorize(Role = Roles.Participant)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyEnrolments()
    {
        var enrolments = await _db.Enrolments
            .WithDetails()
            .Where(e => e.ParticipantID == CurrentUserId)
            .OrderBy(e => e.EnrolmentDate)
            .ToListAsync();

        return Ok(enrolments.Select(e => e.ToResponse()));
    }

    /// <summary>Lists everyone enrolled in a category. Only the Organiser who owns the event can see this.</summary>
    /// <param name="id">The category ID.</param>
    /// <response code="200">The enrolments for that category.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">No category with that ID.</response>
    [HttpGet("api/categories/{id:int}/enrolments")]
    [SessionAuthorize(Role = Roles.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryEnrolments(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
        if (category == null)
            return NotFound(new ErrorResponse("Category not found."));

        if (category.Event.OrganiserID != CurrentUserId)
            return Forbidden("You can only view enrolments for your own events.");

        var enrolments = await _db.Enrolments
            .WithDetails()
            .Where(e => e.CategoryID == id)
            .OrderBy(e => e.EnrolmentDate)
            .ToListAsync();

        return Ok(enrolments.Select(e => e.ToResponse()));
    }

    /// <summary>Cancels the logged-in Participant's own enrolment.</summary>
    /// <param name="id">The enrolment ID.</param>
    /// <response code="204">The enrolment was cancelled.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant, or not your enrolment.</response>
    /// <response code="404">No enrolment with that ID.</response>
    /// <response code="409">A result has already been recorded, so it cannot be cancelled.</response>
    [HttpDelete("api/enrolments/{id:int}")]
    [SessionAuthorize(Role = Roles.Participant)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        var enrolment = await _db.Enrolments.Include(e => e.Result).FirstOrDefaultAsync(e => e.EnrolmentID == id);
        if (enrolment == null)
            return NotFound(new ErrorResponse("Enrolment not found."));

        if (enrolment.ParticipantID != CurrentUserId)
            return Forbidden("You can only cancel your own enrolments.");

        if (enrolment.Result != null)
            return Conflict(new ErrorResponse("A result has already been recorded, so this enrolment cannot be cancelled."));

        _db.Enrolments.Remove(enrolment);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}