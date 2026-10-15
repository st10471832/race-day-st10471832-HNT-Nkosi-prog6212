namespace RaceDay.Api.Models;

public class Participant
{
    public int ParticipantID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<Enrolment> Enrolments { get; set; } = new();
}