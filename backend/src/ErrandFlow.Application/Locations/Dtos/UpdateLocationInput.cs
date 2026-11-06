namespace ErrandFlow.Application.Locations.Dtos;

public sealed record UpdateLocationInput(
    string Label,
    string? Address,
    decimal Latitude,
    decimal Longitude,
    ulong ExpectedRevision);
