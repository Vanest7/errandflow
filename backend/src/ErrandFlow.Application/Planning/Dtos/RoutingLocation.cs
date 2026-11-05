namespace ErrandFlow.Application.Planning.Dtos;

public sealed record RoutingLocation(
    Guid LocationId,
    decimal Latitude,
    decimal Longitude);