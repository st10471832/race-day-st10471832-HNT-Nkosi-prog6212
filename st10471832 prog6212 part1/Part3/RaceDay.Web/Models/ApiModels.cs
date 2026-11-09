namespace RaceDay.Web.Models;

public record ProfileDto(int UserId, string Role, string FullName, string Email, string? Phone,
    DateOnly? DateOfBirth, string? ProfilePictureUrl = null);

public record LoginDto(int UserId, string Role, string FullName, string Message);

public record EventDto(int EventID, int OrganiserID, string OrganiserName, string EventName, string? Description,
    DateOnly EventDate, string Location, decimal DistanceKm, string EventType, string? BannerImageUrl = null);

public record CategoryDto(int CategoryID, int EventID, string CategoryName, decimal DistanceKm,
    int MaxParticipants, decimal Price);

public record EnrolmentDto(int EnrolmentID, int ParticipantID, string ParticipantName, int CategoryID,
    string CategoryName, int EventID, string EventName, DateOnly EventDate, DateTime EnrolmentDate, string Status);

public record ResultDto(int ResultID, int EnrolmentID, int ParticipantID, string ParticipantName, int CategoryID,
    string CategoryName, int EventID, string EventName, TimeSpan? FinishTime, int? Position, string Status);
