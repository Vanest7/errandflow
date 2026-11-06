namespace ErrandFlow.Application.Errands.Dtos;

public sealed record ReorderErrandsResult(
    IReadOnlyList<ErrandDto> Errands,
    ulong PlanRevision);
