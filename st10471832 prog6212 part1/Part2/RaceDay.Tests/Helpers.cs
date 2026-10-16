using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using Xunit;

namespace RaceDay.Tests.Helpers;

public class TestSession : ISession
{
    private readonly Dictionary<string, byte[]> _store = new();

    public string Id => "test-session";
    public bool IsAvailable => true;
    public IEnumerable<string> Keys => _store.Keys;

    public void Clear() => _store.Clear();
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Remove(string key) => _store.Remove(key);
    public void Set(string key, byte[] value) => _store[key] = value;
    public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value) => _store.TryGetValue(key, out value);
}

public static class Reply
{
    public static int Status(IActionResult result) =>
        Assert.IsAssignableFrom<IStatusCodeActionResult>(result).StatusCode ?? 200;

    public static T Body<T>(IActionResult result) =>
        Assert.IsType<T>(Assert.IsAssignableFrom<ObjectResult>(result).Value);

    public static List<T> List<T>(IActionResult result) =>
        Assert.IsAssignableFrom<IEnumerable<T>>(Assert.IsAssignableFrom<ObjectResult>(result).Value).ToList();
}

public static class TestHelpers
{
    private static readonly string DefaultHash = PasswordHelper.Hash("Password123");

    public static RaceDayDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new RaceDayDbContext(options);
    }

    public static T LoggedInAs<T>(this T controller, int userId, string role) where T : ControllerBase
    {
        var http = new DefaultHttpContext { Session = new TestSession() };
        http.Session.SetInt32(SessionKeys.UserId, userId);
        http.Session.SetString(SessionKeys.Role, role);
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        return controller;
    }

    public static T NotLoggedIn<T>(this T controller) where T : ControllerBase
    {
        var http = new DefaultHttpContext { Session = new TestSession() };
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        return controller;
    }

    public static Organiser AddOrganiser(this RaceDayDbContext db,
        string name = "Thabo Nkosi", string email = "thabo@raceday.co.za")
    {
        var organiser = new Organiser { FullName = name, Email = email, PasswordHash = DefaultHash };
        db.Organisers.Add(organiser);
        db.SaveChanges();
        return organiser;
    }

    public static Participant AddParticipant(this RaceDayDbContext db,
        string name = "Lindiwe Dube", string email = "lindiwe@example.com")
    {
        var participant = new Participant
        {
            FullName = name,
            Email = email,
            PasswordHash = DefaultHash,
            DateOfBirth = new DateOnly(1996, 4, 12)
        };
        db.Participants.Add(participant);
        db.SaveChanges();
        return participant;
    }

    public static Event AddEvent(this RaceDayDbContext db, int organiserId, string name = "Benoni City Marathon")
    {
        var ev = new Event
        {
            OrganiserID = organiserId,
            EventName = name,
            Description = "Annual road race.",
            EventDate = new DateOnly(2026, 12, 5),
            Location = "Benoni, Gauteng",
            DistanceKm = 21.1m,
            EventType = "Run"
        };
        db.Events.Add(ev);
        db.SaveChanges();
        return ev;
    }

    public static Category AddCategory(this RaceDayDbContext db, int eventId,
        string name = "21km", int maxParticipants = 100)
    {
        var category = new Category
        {
            EventID = eventId,
            CategoryName = name,
            DistanceKm = 21.1m,
            MaxParticipants = maxParticipants,
            Price = 250m
        };
        db.Categories.Add(category);
        db.SaveChanges();
        return category;
    }

    public static Enrolment AddEnrolment(this RaceDayDbContext db, int categoryId, int participantId)
    {
        var enrolment = new Enrolment { CategoryID = categoryId, ParticipantID = participantId };
        db.Enrolments.Add(enrolment);
        db.SaveChanges();
        return enrolment;
    }
}