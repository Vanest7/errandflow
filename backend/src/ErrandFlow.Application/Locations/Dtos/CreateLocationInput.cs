namespace ErrandFlow.Application.Locations.Dtos;

public sealed record CreateLocationInput(
    string Label,
    string? Address,
    decimal Latitude,
    decimal Longitude,
    ulong ExpectedRevision);