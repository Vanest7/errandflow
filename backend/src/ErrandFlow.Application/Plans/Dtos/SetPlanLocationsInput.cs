namespace ErrandFlow.Application.Plans.Dtos;

public sealed record SetPlanLocationsInput(
    ulong ExpectedRevision,
    Guid? OriginLocationId,
    Guid? DestinationLocationId);
