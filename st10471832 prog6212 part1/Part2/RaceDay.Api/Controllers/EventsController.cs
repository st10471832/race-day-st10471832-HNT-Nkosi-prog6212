using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
public class EventsController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Lists all events (public). Add ?upcomingOnly=true to hide past events.</summary>
    /// <response code="200">A list of events, earliest first.</response>
    [HttpGet("api/events")]
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents([FromQuery] bool upcomingOnly = false)
    {
        IQueryable<Event> query = _db.Events.Include(e => e.Organiser);

        if (upcomingOnly)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            query = query.Where(e => e.EventDate >= today);
        }

        var events = await query.OrderBy(e => e.EventDate).ToListAsync();
        return Ok(events.Select(e => e.ToResponse()));
    }

    /// <summary>Gets the details of one event (public).</summary>
    /// <param name="id">The event ID.</param>
    /// <response code="200">The event.</response>
    /// <response code="404">No event with that ID.</response>
    [HttpGet("api/events/{id:int}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvent(int id)
    {
        var ev = await _db.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventID == id);
        if (ev == null)
            return NotFound(new ErrorResponse("Event not found."));

        return Ok(ev.ToResponse());
    }

    /// <summary>Creates a new event owned by the logged-in Organiser.</summary>
    /// <remarks>EventType must be Run, Walk or Cycle. EventDate format: yyyy-MM-dd.</remarks>
    /// <response code="201">The event was created.</response>
    /// <response code="400">Missing or invalid fields.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only Organisers can create events.</response>
    [HttpPost("api/events")]
    [SessionAuthorize(Role = Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEvent(EventRequest request)
    {
        var organiser = await _db.Organisers.FindAsync(CurrentUserId);
        if (organiser == null)
            return Unauthorized(new ErrorResponse("Your account no longer exists. Please log in again."));

        var ev = new Event
        {
            Organiser = organiser,
            EventName = request.EventName.Trim(),
            Description = request.Description.Trim(),
            EventDate = request.EventDate!.Value,
            Location = request.Location.Trim(),
            DistanceKm = request.DistanceKm,
            EventType = request.EventType
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = ev.EventID }, ev.ToResponse());
    }

    /// <summary>Updates an event. Only the Organiser who created it can do this.</summary>
    /// <param name="id">The event ID.</param>
    /// <response code="200">The updated event.</response>
    /// <response code="400">Missing or invalid fields.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">No event with that ID.</response>
    [HttpPut("api/events/{id:int}")]
    [SessionAuthorize(Role = Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(int id, EventRequest request)
    {
        var ev = await _db.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventID == id);
        if (ev == null)
            return NotFound(new ErrorResponse("Event not found."));

        if (ev.OrganiserID != CurrentUserId)
            return Forbidden("You can only change your own events.");

        ev.EventName = request.EventName.Trim();
        ev.Description = request.Description.Trim();
        ev.EventDate = request.EventDate!.Value;
        ev.Location = request.Location.Trim();
        ev.DistanceKm = request.DistanceKm;
        ev.EventType = request.EventType;

        await _db.SaveChangesAsync();
        return Ok(ev.ToResponse());
    }

    /// <summary>Deletes an event. Only the Organiser who created it can do this.</summary>
    /// <param name="id">The event ID.</param>
    /// <response code="204">The event was deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">No event with that ID.</response>
    /// <response code="409">The event has enrolments, so it cannot be deleted.</response>
    [HttpDelete("api/events/{id:int}")]
    [SessionAuthorize(Role = Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await _db.Events.Include(e => e.Categories).FirstOrDefaultAsync(e => e.EventID == id);
        if (ev == null)
            return NotFound(new ErrorResponse("Event not found."));

        if (ev.OrganiserID != CurrentUserId)
            return Forbidden("You can only delete your own events.");

        var categoryIds = ev.Categories.Select(c => c.CategoryID).ToList();
        if (await _db.Enrolments.AnyAsync(en => categoryIds.Contains(en.CategoryID)))
            return Conflict(new ErrorResponse("This event has enrolments and cannot be deleted."));

        _db.Categories.RemoveRange(ev.Categories);
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}