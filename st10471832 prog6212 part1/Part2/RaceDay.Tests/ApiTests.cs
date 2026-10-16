using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Controllers;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Tests.Helpers;
using Xunit;

namespace RaceDay.Tests;

// ---------- Registration, login and logout ----------
public class AuthTests
{
    [Fact]
    public async Task RegisterOrganiser_WithValidDetails_Returns201AndHashesPassword()
    {
        using var db = TestHelpers.NewDb();
        var controller = new AuthController(db).NotLoggedIn();
        var request = new RegisterOrganiserRequest
        {
            FullName = "Thabo Nkosi",
            Email = "Thabo@RaceDay.co.za",
            Password = "Password123",
            Phone = "0821234567"
        };

        var result = await controller.RegisterOrganiser(request);

        Assert.Equal(201, Reply.Status(result));
        var saved = await db.Organisers.SingleAsync();
        Assert.Equal("thabo@raceday.co.za", saved.Email);
        Assert.NotEqual("Password123", saved.PasswordHash);
        Assert.True(PasswordHelper.Verify(saved.PasswordHash, "Password123"));
    }

    [Fact]
    public async Task RegisterParticipant_WithValidDetails_Returns201()
    {
        using var db = TestHelpers.NewDb();
        var controller = new AuthController(db).NotLoggedIn();
        var request = new RegisterParticipantRequest
        {
            FullName = "Lindiwe Dube",
            Email = "lindiwe@example.com",
            Password = "Password123",
            DateOfBirth = new DateOnly(1996, 4, 12)
        };

        var result = await controller.RegisterParticipant(request);

        Assert.Equal(201, Reply.Status(result));
        Assert.Equal(new DateOnly(1996, 4, 12), (await db.Participants.SingleAsync()).DateOfBirth);
    }

    [Fact]
    public async Task Register_WithEmailAlreadyUsed_Returns409()
    {
        using var db = TestHelpers.NewDb();
        db.AddOrganiser(email: "same@example.com");
        var controller = new AuthController(db).NotLoggedIn();
        var request = new RegisterParticipantRequest
        {
            FullName = "Someone Else",
            Email = "same@example.com",
            Password = "Password123",
            DateOfBirth = new DateOnly(2000, 1, 1)
        };

        var result = await controller.RegisterParticipant(request);

        Assert.Equal(409, Reply.Status(result));
    }

    [Fact]
    public async Task Login_AsOrganiser_StoresUserIdAndRoleInSession()
    {
        using var db = TestHelpers.NewDb();
        var organiser = db.AddOrganiser();
        var controller = new AuthController(db).NotLoggedIn();

        var result = await controller.Login(new LoginRequest { Email = organiser.Email, Password = "Password123" });

        Assert.Equal(200, Reply.Status(result));
        Assert.Equal(organiser.OrganiserID, controller.HttpContext.Session.GetUserId());
        Assert.Equal(Roles.Organiser, controller.HttpContext.Session.GetRole());
    }

    [Fact]
    public async Task Login_AsParticipant_StoresParticipantRole()
    {
        using var db = TestHelpers.NewDb();
        var participant = db.AddParticipant();
        var controller = new AuthController(db).NotLoggedIn();

        var result = await controller.Login(new LoginRequest { Email = participant.Email, Password = "Password123" });

        Assert.Equal(200, Reply.Status(result));
        Assert.Equal(Roles.Participant, controller.HttpContext.Session.GetRole());
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401AndNoSession()
    {
        using var db = TestHelpers.NewDb();
        var organiser = db.AddOrganiser();
        var controller = new AuthController(db).NotLoggedIn();

        var result = await controller.Login(new LoginRequest { Email = organiser.Email, Password = "WrongPassword" });

        Assert.Equal(401, Reply.Status(result));
        Assert.Null(controller.HttpContext.Session.GetUserId());
    }

    [Fact]
    public void Logout_ClearsTheSession()
    {
        using var db = TestHelpers.NewDb();
        var controller = new AuthController(db).LoggedInAs(1, Roles.Organiser);

        var result = controller.Logout();

        Assert.Equal(204, Reply.Status(result));
        Assert.Null(controller.HttpContext.Session.GetUserId());
    }
}

// ---------- Role-based access ----------
public class RoleProtectionTests
{
    private static AuthorizationFilterContext Context(int? userId, string? role)
    {
        var http = new DefaultHttpContext { Session = new TestSession() };
        if (userId != null)
        {
            http.Session.SetInt32(SessionKeys.UserId, userId.Value);
            http.Session.SetString(SessionKeys.Role, role!);
        }
        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    [Fact]
    public void NotLoggedIn_IsRejectedWith401()
    {
        var context = Context(null, null);
        new SessionAuthorizeAttribute { Role = Roles.Organiser }.OnAuthorization(context);

        Assert.NotNull(context.Result);
        Assert.Equal(401, Reply.Status(context.Result!));
    }

    [Fact]
    public void ParticipantOnOrganiserAction_IsRejectedWith403()
    {
        var context = Context(1, Roles.Participant);
        new SessionAuthorizeAttribute { Role = Roles.Organiser }.OnAuthorization(context);

        Assert.Equal(403, Reply.Status(context.Result!));
    }

    [Fact]
    public void OrganiserOnParticipantAction_IsRejectedWith403()
    {
        var context = Context(1, Roles.Organiser);
        new SessionAuthorizeAttribute { Role = Roles.Participant }.OnAuthorization(context);

        Assert.Equal(403, Reply.Status(context.Result!));
    }

    [Fact]
    public void CorrectRole_IsAllowed()
    {
        var context = Context(1, Roles.Organiser);
        new SessionAuthorizeAttribute { Role = Roles.Organiser }.OnAuthorization(context);

        Assert.Null(context.Result);
    }

    public static IEnumerable<object[]> ProtectedActions => new List<object[]>
    {
        new object[] { typeof(EventsController), nameof(EventsController.CreateEvent), Roles.Organiser },
        new object[] { typeof(EventsController), nameof(EventsController.UpdateEvent), Roles.Organiser },
        new object[] { typeof(EventsController), nameof(EventsController.DeleteEvent), Roles.Organiser },
        new object[] { typeof(CategoriesController), nameof(CategoriesController.CreateCategory), Roles.Organiser },
        new object[] { typeof(CategoriesController), nameof(CategoriesController.UpdateCategory), Roles.Organiser },
        new object[] { typeof(CategoriesController), nameof(CategoriesController.DeleteCategory), Roles.Organiser },
        new object[] { typeof(EnrolmentsController), nameof(EnrolmentsController.GetCategoryEnrolments), Roles.Organiser },
        new object[] { typeof(ResultsController), nameof(ResultsController.CaptureResult), Roles.Organiser },
        new object[] { typeof(EnrolmentsController), nameof(EnrolmentsController.Enrol), Roles.Participant },
        new object[] { typeof(EnrolmentsController), nameof(EnrolmentsController.GetMyEnrolments), Roles.Participant },
        new object[] { typeof(EnrolmentsController), nameof(EnrolmentsController.CancelEnrolment), Roles.Participant },
        new object[] { typeof(ResultsController), nameof(ResultsController.GetMyResults), Roles.Participant },
        new object[] { typeof(UsersController), nameof(UsersController.GetMyProfile), null! },
        new object[] { typeof(UsersController), nameof(UsersController.UpdateMyProfile), null! },
    };

    [Theory]
    [MemberData(nameof(ProtectedActions))]
    public void Endpoint_RequiresTheExpectedRole(Type controller, string action, string? expectedRole)
    {
        var method = controller.GetMethod(action);
        Assert.NotNull(method);

        var attribute = method!.GetCustomAttribute<SessionAuthorizeAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(expectedRole, attribute!.Role);
    }

    [Fact]
    public void EveryNonPublicEndpoint_RequiresLogin()
    {
        var publicActions = new HashSet<string>
        {
            "AuthController.RegisterOrganiser", "AuthController.RegisterParticipant",
            "AuthController.Login", "AuthController.Logout",
            "EventsController.GetEvents", "EventsController.GetEvent",
            "CategoriesController.GetCategories", "ResultsController.GetCategoryResults"
        };

        var controllers = typeof(AuthController).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract);

        foreach (var controller in controllers)
        {
            var actions = controller
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any());

            foreach (var action in actions)
            {
                var name = $"{controller.Name}.{action.Name}";
                if (publicActions.Contains(name)) continue;

                Assert.True(action.GetCustomAttribute<SessionAuthorizeAttribute>() != null,
                    $"{name} must require login");
            }
        }
    }
}

// ---------- Events ----------
public class EventTests
{
    private static EventRequest ValidEvent(string name = "Benoni City Marathon") => new()
    {
        EventName = name,
        Description = "Annual road race.",
        EventDate = new DateOnly(2026, 12, 5),
        Location = "Benoni, Gauteng",
        DistanceKm = 21.1m,
        EventType = "Run"
    };

    [Fact]
    public async Task CreateEvent_AsOrganiser_Returns201AndLinksToOrganiser()
    {
        using var db = TestHelpers.NewDb();
        var organiser = db.AddOrganiser();
        var controller = new EventsController(db).LoggedInAs(organiser.OrganiserID, Roles.Organiser);

        var result = await controller.CreateEvent(ValidEvent());

        Assert.Equal(201, Reply.Status(result));
        var saved = await db.Events.SingleAsync();
        Assert.Equal(organiser.OrganiserID, saved.OrganiserID);
        Assert.Equal("Run", saved.EventType);
        Assert.Equal("Thabo Nkosi", Reply.Body<EventResponse>(result).OrganiserName);
    }

    [Fact]
    public async Task GetEvents_ReturnsAllEvents()
    {
        using var db = TestHelpers.NewDb();
        var organiser = db.AddOrganiser();
        db.AddEvent(organiser.OrganiserID, "Event One");
        db.AddEvent(organiser.OrganiserID, "Event Two");
        var controller = new EventsController(db).NotLoggedIn();

        var result = await controller.GetEvents();

        Assert.Equal(200, Reply.Status(result));
        Assert.Equal(2, Reply.List<EventResponse>(result).Count);
    }

    [Fact]
    public async Task GetEvent_Unknown_Returns404()
    {
        using var db = TestHelpers.NewDb();
        var controller = new EventsController(db).NotLoggedIn();

        var result = await controller.GetEvent(999);

        Assert.Equal(404, Reply.Status(result));
    }

    [Fact]
    public async Task UpdateEvent_ByOwner_Returns200AndChangesEvent()
    {
        using var db = TestHelpers.NewDb();
        var organiser = db.AddOrganiser();
        var ev = db.AddEvent(organiser.OrganiserID);
        var controller = new EventsController(db).LoggedInAs(organiser.OrganiserID, Roles.Organiser);

        var result = await controller.UpdateEvent(ev.EventID, ValidEvent("Renamed Marathon"));

        Assert.Equal(200, Reply.Status(result));
        Assert.Equal("Renamed Marathon", (await db.Events.SingleAsync()).EventName);
    }

    [Fact]
    public async Task UpdateEvent_ByAnotherOrganiser_Returns403AndChangesNothing()
    {
        using var db = TestHelpers.NewDb();
        var owner = db.AddOrganiser();
        var other = db.AddOrganiser("Sarah van Wyk", "sarah@raceday.co.za");
        var ev = db.AddEvent(owner.OrganiserID);
        var controller = new EventsController(db).LoggedInAs(other.OrganiserID, Roles.Organiser);

        var result = await controller.UpdateEvent(ev.EventID, ValidEvent("Hijacked"));

        Assert.Equal(403, Reply.Status(result));
        Assert.Equal("Benoni City Marathon", (await db.Events.SingleAsync()).EventName);
    }

    [Fact]
    public async Task DeleteEvent_ByOwner_Returns204()
    {
        using var db = TestHelpers.NewDb();
        var organiser = db.AddOrganiser();
        var ev = db.AddEvent(organiser.OrganiserID);
        var controller = new EventsController(db).LoggedInAs(organiser.OrganiserID, Roles.Organiser);

        var result = await controller.DeleteEvent(ev.EventID);

        Assert.Equal(204, Reply.Status(result));
        Assert.Empty(await db.Events.ToListAsync());
    }

    [Fact]
    public async Task DeleteEvent_ByAnotherOrganiser_Returns403()
    {
        using var db = TestHelpers.NewDb();
        var owner = db.AddOrganiser();
        var other = db.AddOrganiser("Sarah van Wyk", "sarah@raceday.co.za");
        var ev = db.AddEvent(owner.OrganiserID);
        var controller = new EventsController(db).LoggedInAs(other.OrganiserID, Roles.Organiser);

        var result = await controller.DeleteEvent(ev.EventID);

        Assert.Equal(403, Reply.Status(result));
        Assert.Single(await db.Events.ToListAsync());
    }

    [Fact]
    public async Task DeleteEvent_WithEnrolments_Returns409()
    {
        using var db = TestHelpers.NewDb();
        var organiser = db.AddOrganiser();
        var participant = db.AddParticipant();
        var ev = db.AddEvent(organiser.OrganiserID);
        var category = db.AddCategory(ev.EventID);
        db.AddEnrolment(category.CategoryID, participant.ParticipantID);
        var controller = new EventsController(db).LoggedInAs(organiser.OrganiserID, Roles.Organiser);

        var result = await controller.DeleteEvent(ev.EventID);

        Assert.Equal(409, Reply.Status(result));
    }
}

// ---------- Profile ----------
public class ProfileTests
{
    [Fact]
    public async Task GetMyProfile_ReturnsLoggedInUser()
    {
        using var db = TestHelpers.NewDb();
        var organiser = db.AddOrganiser();
        var controller = new UsersController(db).LoggedInAs(organiser.OrganiserID, Roles.Organiser);

        var result = await controller.GetMyProfile();

        Assert.Equal(200, Reply.Status(result));
        var profile = Reply.Body<ProfileResponse>(result);
        Assert.Equal(Roles.Organiser, profile.Role);
        Assert.Equal("Thabo Nkosi", profile.FullName);
    }

    [Fact]
    public async Task UpdateMyProfile_ChangesName()
    {
        using var db = TestHelpers.NewDb();
        var participant = db.AddParticipant();
        var controller = new UsersController(db).LoggedInAs(participant.ParticipantID, Roles.Participant);

        var result = await controller.UpdateMyProfile(new UpdateProfileRequest { FullName = "Lindiwe Updated", Phone = "0790000000" });

        Assert.Equal(200, Reply.Status(result));
        Assert.Equal("Lindiwe Updated", (await db.Participants.SingleAsync()).FullName);
    }
}

// ---------- Enrolments ----------
public class EnrolmentTests
{
    private static (RaceDayDbContext Db, Organiser Organiser, Participant Participant, Category Category) Setup(int maxParticipants = 100)
    {
        var db = TestHelpers.NewDb();
        var organiser = db.AddOrganiser();
        var participant = db.AddParticipant();
        var ev = db.AddEvent(organiser.OrganiserID);
        var category = db.AddCategory(ev.EventID, maxParticipants: maxParticipants);
        return (db, organiser, participant, category);
    }

    [Fact]
    public async Task Enrol_AsParticipant_RecordsParticipantCategoryAndEvent()
    {
        var (db, _, participant, category) = Setup();
        var controller = new EnrolmentsController(db).LoggedInAs(participant.ParticipantID, Roles.Participant);

        var result = await controller.Enrol(category.CategoryID);

        Assert.Equal(201, Reply.Status(result));
        var saved = await db.Enrolments.SingleAsync();
        Assert.Equal(participant.ParticipantID, saved.ParticipantID);
        Assert.Equal(category.CategoryID, saved.CategoryID);
        var body = Reply.Body<EnrolmentResponse>(result);
        Assert.Equal(category.EventID, body.EventID);
        Assert.Equal("Confirmed", body.Status);
    }

    [Fact]
    public async Task Enrol_Twice_Returns409()
    {
        var (db, _, participant, category) = Setup();
        db.AddEnrolment(category.CategoryID, participant.ParticipantID);
        var controller = new EnrolmentsController(db).LoggedInAs(participant.ParticipantID, Roles.Participant);

        var result = await controller.Enrol(category.CategoryID);

        Assert.Equal(409, Reply.Status(result));
    }

    [Fact]
    public async Task Enrol_WhenCategoryIsFull_Returns409()
    {
        var (db, _, participant, category) = Setup(maxParticipants: 1);
        var other = db.AddParticipant("Other Runner", "other@example.com");
        db.AddEnrolment(category.CategoryID, other.ParticipantID);
        var controller = new EnrolmentsController(db).LoggedInAs(participant.ParticipantID, Roles.Participant);

        var result = await controller.Enrol(category.CategoryID);

        Assert.Equal(409, Reply.Status(result));
    }

    [Fact]
    public async Task Enrol_UnknownCategory_Returns404()
    {
        var (db, _, participant, _) = Setup();
        var controller = new EnrolmentsController(db).LoggedInAs(participant.ParticipantID, Roles.Participant);

        var result = await controller.Enrol(999);

        Assert.Equal(404, Reply.Status(result));
    }

    [Fact]
    public async Task GetMyEnrolments_ReturnsOnlyTheLoggedInParticipantsEnrolments()
    {
        var (db, _, participant, category) = Setup();
        var other = db.AddParticipant("Other Runner", "other@example.com");
        db.AddEnrolment(category.CategoryID, participant.ParticipantID);
        db.AddEnrolment(category.CategoryID, other.ParticipantID);
        var controller = new EnrolmentsController(db).LoggedInAs(participant.ParticipantID, Roles.Participant);

        var result = await controller.GetMyEnrolments();

        var list = Reply.List<EnrolmentResponse>(result);
        Assert.Single(list);
        Assert.Equal(participant.ParticipantID, list[0].ParticipantID);
    }

    [Fact]
    public async Task GetCategoryEnrolments_ByOwningOrganiser_ReturnsEnrolments()
    {
        var (db, organiser, participant, category) = Setup();
        db.AddEnrolment(category.CategoryID, participant.ParticipantID);
        var controller = new EnrolmentsController(db).LoggedInAs(organiser.OrganiserID, Roles.Organiser);

        var result = await controller.GetCategoryEnrolments(category.CategoryID);

        Assert.Equal(200, Reply.Status(result));
        Assert.Single(Reply.List<EnrolmentResponse>(result));
    }

    [Fact]
    public async Task GetCategoryEnrolments_ByAnotherOrganiser_Returns403()
    {
        var (db, _, participant, category) = Setup();
        var other = db.AddOrganiser("Sarah van Wyk", "sarah@raceday.co.za");
        db.AddEnrolment(category.CategoryID, participant.ParticipantID);
        var controller = new EnrolmentsController(db).LoggedInAs(other.OrganiserID, Roles.Organiser);

        var result = await controller.GetCategoryEnrolments(category.CategoryID);

        Assert.Equal(403, Reply.Status(result));
    }

    [Fact]
    public async Task CancelEnrolment_OwnEnrolment_Returns204()
    {
        var (db, _, participant, category) = Setup();
        var enrolment = db.AddEnrolment(category.CategoryID, participant.ParticipantID);
        var controller = new EnrolmentsController(db).LoggedInAs(participant.ParticipantID, Roles.Participant);

        var result = await controller.CancelEnrolment(enrolment.EnrolmentID);

        Assert.Equal(204, Reply.Status(result));
        Assert.Empty(await db.Enrolments.ToListAsync());
    }

    [Fact]
    public async Task CancelEnrolment_SomeoneElsesEnrolment_Returns403()
    {
        var (db, _, participant, category) = Setup();
        var other = db.AddParticipant("Other Runner", "other@example.com");
        var enrolment = db.AddEnrolment(category.CategoryID, participant.ParticipantID);
        var controller = new EnrolmentsController(db).LoggedInAs(other.ParticipantID, Roles.Participant);

        var result = await controller.CancelEnrolment(enrolment.EnrolmentID);

        Assert.Equal(403, Reply.Status(result));
        Assert.Single(await db.Enrolments.ToListAsync());
    }
}

// ---------- Results ----------
public class ResultTests
{
    private static ResultRequest Finished(int position = 3) => new()
    {
        FinishTime = TimeSpan.FromMinutes(52),
        Position = position,
        Status = "Finished"
    };

    private static (RaceDayDbContext Db, Organiser Organiser, Enrolment Enrolment) Setup()
    {
        var db = TestHelpers.NewDb();
        var organiser = db.AddOrganiser();
        var participant = db.AddParticipant();
        var ev = db.AddEvent(organiser.OrganiserID);
        var category = db.AddCategory(ev.EventID);
        var enrolment = db.AddEnrolment(category.CategoryID, participant.ParticipantID);
        return (db, organiser, enrolment);
    }

    [Fact]
    public async Task CaptureResult_ByOwningOrganiser_Returns201()
    {
        var (db, organiser, enrolment) = Setup();
        var controller = new ResultsController(db).LoggedInAs(organiser.OrganiserID, Roles.Organiser);

        var result = await controller.CaptureResult(enrolment.EnrolmentID, Finished());

        Assert.Equal(201, Reply.Status(result));
        var saved = await db.Results.SingleAsync();
        Assert.Equal(3, saved.Position);
        Assert.Equal(TimeSpan.FromMinutes(52), saved.FinishTime);
    }

    [Fact]
    public async Task CaptureResult_ByAnotherOrganiser_Returns403()
    {
        var (db, _, enrolment) = Setup();
        var other = db.AddOrganiser("Sarah van Wyk", "sarah@raceday.co.za");
        var controller = new ResultsController(db).LoggedInAs(other.OrganiserID, Roles.Organiser);

        var result = await controller.CaptureResult(enrolment.EnrolmentID, Finished());

        Assert.Equal(403, Reply.Status(result));
        Assert.Empty(await db.Results.ToListAsync());
    }

    [Fact]
    public async Task CaptureResult_FinishedWithoutTime_Returns400()
    {
        var (db, organiser, enrolment) = Setup();
        var controller = new ResultsController(db).LoggedInAs(organiser.OrganiserID, Roles.Organiser);

        var result = await controller.CaptureResult(enrolment.EnrolmentID, new ResultRequest { Status = "Finished" });

        Assert.Equal(400, Reply.Status(result));
    }

    [Fact]
    public async Task CaptureResult_Twice_Returns409()
    {
        var (db, organiser, enrolment) = Setup();
        var controller = new ResultsController(db).LoggedInAs(organiser.OrganiserID, Roles.Organiser);
        await controller.CaptureResult(enrolment.EnrolmentID, Finished());

        var second = await controller.CaptureResult(enrolment.EnrolmentID, Finished());

        Assert.Equal(409, Reply.Status(second));
    }

    [Fact]
    public async Task GetCategoryResults_AreOrderedByPosition()
    {
        var (db, organiser, firstEnrolment) = Setup();
        var other = db.AddParticipant("Other Runner", "other@example.com");
        var secondEnrolment = db.AddEnrolment(firstEnrolment.CategoryID, other.ParticipantID);
        db.Results.Add(new Result { EnrolmentID = firstEnrolment.EnrolmentID, Position = 2, FinishTime = TimeSpan.FromMinutes(55), Status = "Finished" });
        db.Results.Add(new Result { EnrolmentID = secondEnrolment.EnrolmentID, Position = 1, FinishTime = TimeSpan.FromMinutes(50), Status = "Finished" });
        db.SaveChanges();
        var controller = new ResultsController(db).NotLoggedIn();

        var result = await controller.GetCategoryResults(firstEnrolment.CategoryID);

        var list = Reply.List<ResultResponse>(result);
        Assert.Equal(2, list.Count);
        Assert.Equal(1, list[0].Position);
        Assert.Equal(2, list[1].Position);
    }
}