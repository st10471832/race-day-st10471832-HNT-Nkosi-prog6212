namespace RaceDay.Api.Models;

public class Enrolment
{
    public int EnrolmentID { get; set; }
    public int CategoryID { get; set; }
    public int ParticipantID { get; set; }
    public DateTime EnrolmentDate { get; set; } = DateTime.Now;
    public string Status { get; set; } = "Confirmed";

    public Category Category { get; set; } = null!;
    public Participant Participant { get; set; } = null!;
    public Result? Result { get; set; }
}