namespace ErrandFlow.Application.Locations.Dtos;

public sealed record LocationDto(
    Guid Id,
    Guid PlanId,
    string Label,
    string? Address,
    decimal Latitude,
    decimal Longitude);