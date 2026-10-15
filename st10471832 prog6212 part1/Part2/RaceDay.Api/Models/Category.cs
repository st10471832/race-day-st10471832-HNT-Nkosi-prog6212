namespace RaceDay.Api.Models;

public class Category
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; } = 100;
    public decimal Price { get; set; }

    public Event Event { get; set; } = null!;
    public List<Enrolment> Enrolments { get; set; } = new();
}