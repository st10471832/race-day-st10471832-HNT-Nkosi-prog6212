namespace RaceDay.Api.Dtos;

public record ErrorResponse(string Message);

public record ProfileResponse(int UserId, string Role, string FullName, string Email, string? Phone, DateOnly? DateOfBirth);

public record LoginResponse(int UserId, string Role, string FullName, string Message);

public record EventResponse(
    int EventID, int OrganiserID, string OrganiserName, string EventName, string? Description,
    DateOnly EventDate, string Location, decimal DistanceKm, string EventType);

public record CategoryResponse(
    int CategoryID, int EventID, string CategoryName, decimal DistanceKm, int MaxParticipants, decimal Price);

public record EnrolmentResponse(
    int EnrolmentID, int ParticipantID, string ParticipantName, int CategoryID, string CategoryName,
    int EventID, string EventName, DateOnly EventDate, DateTime EnrolmentDate, string Status);

public record ResultResponse(
    int ResultID, int EnrolmentID, int ParticipantID, string ParticipantName, int CategoryID, string CategoryName,
    int EventID, string EventName, TimeSpan? FinishTime, int? Position, string Status);