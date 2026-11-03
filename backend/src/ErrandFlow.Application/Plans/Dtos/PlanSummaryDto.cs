namespace ErrandFlow.Application.Plans.Dtos;

public sealed record PlanSummaryDto(
    Guid Id,
    string Name,
    DateOnly PlanDate,
    TimeOnly DepartureTime,
    string TimeZone
);