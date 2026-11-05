namespace ErrandFlow.Application.Plans.Dtos;

public sealed record PlanDto(
    Guid Id,
    Guid OwnerId,
    string Name,
    DateOnly PlanDate,
    TimeOnly DepartureTime,
    string TimeZone,
    Guid? OriginLocationId,
    Guid? DestinationLocationId,
    ulong Revision,
    DateTime CreatedAt,
    DateTime UpdatedAt);
