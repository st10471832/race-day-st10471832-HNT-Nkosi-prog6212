using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
public class CategoriesController : RaceDayControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Lists all race categories for an event (public).</summary>
    /// <param name="eventId">The event ID.</param>
    /// <response code="200">The categories for that event.</response>
    /// <response code="404">No event with that ID.</response>
    [HttpGet("api/events/{eventId:int}/categories")]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategories(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventID == eventId))
            return NotFound(new ErrorResponse("Event not found."));

        var categories = await _db.Categories
            .Where(c => c.EventID == eventId)
            .OrderBy(c => c.CategoryID)
            .ToListAsync();

        return Ok(categories.Select(c => c.ToResponse()));
    }

    /// <summary>Adds a category (for example Under 20, Senior, 10km, 21km) to one of your events.</summary>
    /// <param name="eventId">The event ID.</param>
    /// <response code="201">The category was created.</response>
    /// <response code="400">Missing or invalid fields.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">No event with that ID.</response>
    [HttpPost("api/events/{eventId:int}/categories")]
    [SessionAuthorize(Role = Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateCategory(int eventId, CategoryRequest request)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventID == eventId);
        if (ev == null)
            return NotFound(new ErrorResponse("Event not found."));

        if (ev.OrganiserID != CurrentUserId)
            return Forbidden("You can only add categories to your own events.");

        var category = new Category
        {
            EventID = eventId,
            CategoryName = request.CategoryName.Trim(),
            DistanceKm = request.DistanceKm,
            MaxParticipants = request.MaxParticipants,
            Price = request.Price
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return Created($"/api/events/{eventId}/categories", category.ToResponse());
    }

    /// <summary>Updates a category on one of your events.</summary>
    /// <param name="id">The category ID.</param>
    /// <response code="200">The updated category.</response>
    /// <response code="400">Missing or invalid fields.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">No category with that ID.</response>
    [HttpPut("api/categories/{id:int}")]
    [SessionAuthorize(Role = Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(int id, CategoryRequest request)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
        if (category == null)
            return NotFound(new ErrorResponse("Category not found."));

        if (category.Event.OrganiserID != CurrentUserId)
            return Forbidden("You can only change categories on your own events.");

        category.CategoryName = request.CategoryName.Trim();
        category.DistanceKm = request.DistanceKm;
        category.MaxParticipants = request.MaxParticipants;
        category.Price = request.Price;

        await _db.SaveChangesAsync();
        return Ok(category.ToResponse());
    }

    /// <summary>Deletes a category from one of your events.</summary>
    /// <param name="id">The category ID.</param>
    /// <response code="204">The category was deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">No category with that ID.</response>
    /// <response code="409">The category has enrolments, so it cannot be deleted.</response>
    [HttpDelete("api/categories/{id:int}")]
    [SessionAuthorize(Role = Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
        if (category == null)
            return NotFound(new ErrorResponse("Category not found."));

        if (category.Event.OrganiserID != CurrentUserId)
            return Forbidden("You can only delete categories on your own events.");

        if (await _db.Enrolments.AnyAsync(en => en.CategoryID == id))
            return Conflict(new ErrorResponse("This category has enrolments and cannot be deleted."));

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}