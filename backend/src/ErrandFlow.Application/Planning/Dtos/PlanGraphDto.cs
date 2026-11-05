namespace ErrandFlow.Application.Planning.Dtos;

public sealed record PlanGraphDto(
    Guid PlanId,
    ulong Revision,
    string TravelMode,
    Guid? OriginLocationId,
    Guid? DestinationLocationId,
    IReadOnlyList<Guid> LocationIds,
    IReadOnlyList<TravelConnectionDto> Connections);