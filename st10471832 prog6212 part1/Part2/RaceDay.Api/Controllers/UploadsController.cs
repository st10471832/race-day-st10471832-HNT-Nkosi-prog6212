using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Controllers;

[ApiController]
public class UploadsController : RaceDayControllerBase
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private const long MaxBytes = 5 * 1024 * 1024;

    private readonly RaceDayDbContext _db;
    private readonly IWebHostEnvironment _env;

    public UploadsController(RaceDayDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpPost("api/events/{id:int}/banner")]
    [SessionAuthorize(Role = Roles.Organiser)]
    public async Task<IActionResult> UploadBanner(int id, IFormFile file)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null)
            return NotFound(new ErrorResponse("Event not found."));

        if (ev.OrganiserID != CurrentUserId)
            return Forbidden("You can only change your own events.");

        var saved = await SaveImage(file, "banners");
        if (saved.Error != null)
            return BadRequest(new ErrorResponse(saved.Error));

        ev.BannerImageUrl = saved.Url;
        await _db.SaveChangesAsync();
        return Ok(new { url = saved.Url });
    }

    [HttpPost("api/users/me/picture")]
    [SessionAuthorize]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        var saved = await SaveImage(file, "profiles");
        if (saved.Error != null)
            return BadRequest(new ErrorResponse(saved.Error));

        var role = HttpContext.Session.GetString("Role");

        if (role == Roles.Organiser)
        {
            var organiser = await _db.Organisers.FindAsync(CurrentUserId);
            if (organiser == null)
                return Unauthorized(new ErrorResponse("Your account no longer exists. Please log in again."));
            organiser.ProfilePictureUrl = saved.Url;
        }
        else
        {
            var participant = await _db.Participants.FindAsync(CurrentUserId);
            if (participant == null)
                return Unauthorized(new ErrorResponse("Your account no longer exists. Please log in again."));
            participant.ProfilePictureUrl = saved.Url;
        }

        await _db.SaveChangesAsync();
        return Ok(new { url = saved.Url });
    }

    private async Task<(string? Url, string? Error)> SaveImage(IFormFile? file, string folder)
    {
        if (file == null || file.Length == 0)
            return (null, "Please choose an image to upload.");

        if (file.Length > MaxBytes)
            return (null, "The image must be 5 MB or smaller.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return (null, "Only JPG, PNG, GIF or WEBP images are allowed.");

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var targetFolder = Path.Combine(webRoot, "uploads", folder);
        Directory.CreateDirectory(targetFolder);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(targetFolder, fileName);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var url = $"{Request.Scheme}://{Request.Host}/uploads/{folder}/{fileName}";
        return (url, null);
    }
}