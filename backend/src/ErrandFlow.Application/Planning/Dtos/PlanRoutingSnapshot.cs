namespace ErrandFlow.Application.Planning.Dtos;

public sealed record PlanRoutingSnapshot(
    Guid PlanId,
    ulong Revision,
    Guid? OriginLocationId,
    Guid? DestinationLocationId,
    IReadOnlyList<RoutingLocation> Locations);