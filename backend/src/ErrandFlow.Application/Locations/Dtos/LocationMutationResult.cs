namespace ErrandFlow.Application.Locations.Dtos;

public sealed record LocationMutationResult(
    LocationDto Location,
    ulong PlanRevision);