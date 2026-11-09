namespace RaceDay.Web.Models;

public class EventStat
{
    public EventDto Event { get; set; } = null!;
    public int Enrolments { get; set; }
}

public class DashboardViewModel
{
    public List<EventStat> Events { get; set; } = new();
    public int TotalEnrolments => Events.Sum(e => e.Enrolments);
    public int UpcomingCount => Events.Count(e => e.Event.EventDate >= DateOnly.FromDateTime(DateTime.Today));
}