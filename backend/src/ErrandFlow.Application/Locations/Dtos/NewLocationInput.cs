namespace ErrandFlow.Application.Locations.Dtos;

public sealed record NewLocationInput(
    string Label,
    string? Address,
    decimal Latitude,
    decimal Longitude);