using ErrandFlow.Domain.Planning;

namespace ErrandFlow.Application.Planning.Dtos;

public sealed record PlanGraphResult(
    Guid PlanId,
    ulong Revision,
    TravelMode TravelMode,
    Guid? OriginLocationId,
    Guid? DestinationLocationId,
    LocationGraph Graph);