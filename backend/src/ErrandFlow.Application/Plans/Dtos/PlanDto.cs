namespace ErrandFlow.Application.Plans.Dtos;

public sealed record PlanDto(
    Guid Id,
    Guid OwnerId,
    string Name,
    DateOnly PlanDate,
    TimeOnly DepartureTime,
    string TimeZone,
    ulong Revision,
    DateTime CreatedAt,
    DateTime UpdatedAt);
