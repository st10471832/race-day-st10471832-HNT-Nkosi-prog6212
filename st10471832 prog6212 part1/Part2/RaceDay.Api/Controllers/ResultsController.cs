using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
public class ResultsController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Records a finish time and position for an enrolment. Only the Organiser who owns the event can do this.</summary>
    /// <remarks>Status must be Finished, DNF, DNS or Disqualified. A Finished result needs a FinishTime (hh:mm:ss) and a Position.</remarks>
    /// <param name="id">The enrolment ID.</param>
    /// <response code="201">The result was recorded.</response>
    /// <response code="400">Missing or invalid fields.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">No enrolment with that ID.</response>
    /// <response code="409">A result has already been recorded for this enrolment.</response>
    [HttpPost("api/enrolments/{id:int}/result")]
    [SessionAuthorize(Role = Roles.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CaptureResult(int id, ResultRequest request)
    {
        var enrolment = await _db.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrolmentID == id);

        if (enrolment == null)
            return NotFound(new ErrorResponse("Enrolment not found."));

        if (enrolment.Category.Event.OrganiserID != CurrentUserId)
            return Forbidden("You can only record results for your own events.");

        if (enrolment.Result != null)
            return Conflict(new ErrorResponse("A result has already been recorded for this enrolment."));

        if (request.FinishTime < TimeSpan.Zero)
            return BadRequest(new ErrorResponse("Finish time cannot be negative."));

        if (request.Status == "Finished" && (request.FinishTime == null || request.Position == null))
            return BadRequest(new ErrorResponse("A finished result needs a finish time and a position."));

        var result = new Result
        {
            Enrolment = enrolment,
            FinishTime = request.FinishTime,
            Position = request.Position,
            Status = request.Status
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        return Created($"/api/categories/{enrolment.CategoryID}/results", result.ToResponse());
    }

    /// <summary>Shows the results (leaderboard) for a category, ordered by position (public).</summary>
    /// <param name="id">The category ID.</param>
    /// <response code="200">The results for that category.</response>
    /// <response code="404">No category with that ID.</response>
    [HttpGet("api/categories/{id:int}/results")]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryResults(int id)
    {
        if (!await _db.Categories.AnyAsync(c => c.CategoryID == id))
            return NotFound(new ErrorResponse("Category not found."));

        var results = await _db.Results
            .WithDetails()
            .Where(r => r.Enrolment.CategoryID == id)
            .ToListAsync();

        return Ok(results.OrderBy(r => r.Position ?? int.MaxValue).Select(r => r.ToResponse()));
    }

    /// <summary>Lists the logged-in Participant's own race results.</summary>
    /// <response code="200">Your results.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only Participants have personal results.</response>
    [HttpGet("api/participants/me/results")]
    [SessionAuthorize(Role = Roles.Participant)]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyResults()
    {
        var results = await _db.Results
            .WithDetails()
            .Where(r => r.Enrolment.ParticipantID == CurrentUserId)
            .ToListAsync();

        return Ok(results.OrderBy(r => r.Position ?? int.MaxValue).Select(r => r.ToResponse()));
    }
}