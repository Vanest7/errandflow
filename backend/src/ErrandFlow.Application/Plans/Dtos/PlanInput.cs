namespace ErrandFlow.Application.Plans.Dtos;

public sealed record PlanInput(
    [property: System.ComponentModel.DataAnnotations.Required]
    [property: System.ComponentModel.DataAnnotations.MaxLength(150)]
    string Name,
    DateOnly PlanDate,
    TimeOnly DepartureTime,
    string TimeZone = "Europe/Madrid");

public sealed record UpdatePlanInput(
    [property: System.ComponentModel.DataAnnotations.Required]
    [property: System.ComponentModel.DataAnnotations.MaxLength(150)]
    string Name,
    DateOnly PlanDate,
    TimeOnly DepartureTime,
    string TimeZone,
    ulong ExpectedRevision);
