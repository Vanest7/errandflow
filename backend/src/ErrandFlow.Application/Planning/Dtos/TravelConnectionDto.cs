namespace ErrandFlow.Application.Planning.Dtos;

public sealed record TravelConnectionDto(
    Guid FromLocationId,
    Guid ToLocationId,
    bool IsReachable,
    double? DurationSeconds,
    decimal? DistanceMeters);