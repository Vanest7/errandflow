namespace ErrandFlow.Application.Places.Dtos;

public sealed record PlaceSearchResultDto(
    string? ProviderPlaceId,
    string Label,
    string? Address,
    decimal Latitude,
    decimal Longitude);
