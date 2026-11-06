namespace ErrandFlow.Application.Planning.Dtos;

public sealed record RoutePathDto(
    IReadOnlyList<RouteLegDto> Legs)
{
    public double TotalDistanceMeters =>
        Legs.Sum(leg => leg.DistanceMeters);

    public double TotalTravelDurationSeconds =>
        Legs.Sum(leg => leg.TravelDurationSeconds);
}