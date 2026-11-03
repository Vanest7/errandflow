namespace ErrandFlow.Application.Plans.Dtos;

public sealed record PlanInput(
    string Name,
    DateOnly PlanDate,
    TimeOnly DepartureTime,
    string TimeZone = "Europe/Madrid");
