namespace ErrandFlow.Application.Errands.Dtos;

public sealed record ErrandMutationResult(
    ErrandDto Errand,
    ulong PlanRevision);