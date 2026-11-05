using System.ComponentModel.DataAnnotations;

namespace ErrandFlow.Application.Plans.Dtos;

public sealed record PlanInput(
    [Required]
    [MaxLength(150)]
    string Name,
    DateOnly PlanDate,
    TimeOnly DepartureTime,
    string TimeZone = "Europe/Madrid");

public sealed record UpdatePlanInput(
    [Required]
    [MaxLength(150)]
    string Name,
    DateOnly PlanDate,
    TimeOnly DepartureTime,
    string TimeZone,
    ulong ExpectedRevision);