namespace ErrandFlow.Application.Places.Dtos;

public sealed record PlaceSearchInput(
    string? Query = null,
    string? Category = null,
    decimal? Latitude = null,
    decimal? Longitude = null,
    int Limit = 5,
    decimal RadiusKilometers = 3,
    string? BoundingBox = null);
