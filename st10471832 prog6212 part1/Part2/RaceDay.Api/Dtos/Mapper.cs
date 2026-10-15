using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public static class Mapper
{
    public static EventResponse ToResponse(this Event e) =>
        new(e.EventID, e.OrganiserID, e.Organiser.FullName, e.EventName, e.Description,
            e.EventDate, e.Location, e.DistanceKm, e.EventType);

    public static CategoryResponse ToResponse(this Category c) =>
        new(c.CategoryID, c.EventID, c.CategoryName, c.DistanceKm, c.MaxParticipants, c.Price);

    public static EnrolmentResponse ToResponse(this Enrolment en) =>
        new(en.EnrolmentID, en.ParticipantID, en.Participant.FullName, en.CategoryID, en.Category.CategoryName,
            en.Category.EventID, en.Category.Event.EventName, en.Category.Event.EventDate,
            en.EnrolmentDate, en.Status);

    public static ResultResponse ToResponse(this Result r) =>
        new(r.ResultID, r.EnrolmentID, r.Enrolment.ParticipantID, r.Enrolment.Participant.FullName,
            r.Enrolment.CategoryID, r.Enrolment.Category.CategoryName,
            r.Enrolment.Category.EventID, r.Enrolment.Category.Event.EventName,
            r.FinishTime, r.Position, r.Status);

    public static IQueryable<Enrolment> WithDetails(this IQueryable<Enrolment> query) =>
        query.Include(e => e.Participant)
             .Include(e => e.Category).ThenInclude(c => c.Event);

    public static IQueryable<Result> WithDetails(this IQueryable<Result> query) =>
        query.Include(r => r.Enrolment).ThenInclude(e => e.Participant)
             .Include(r => r.Enrolment).ThenInclude(e => e.Category).ThenInclude(c => c.Event);
}