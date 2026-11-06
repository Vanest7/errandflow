namespace ErrandFlow.Application.Planning.Dtos;

public sealed record RouteLegDto(
    Guid FromLocationId,
    Guid ToLocationId,
    double DistanceMeters,
    double TravelDurationSeconds,
    IReadOnlyList<RouteCoordinate> Coordinates);