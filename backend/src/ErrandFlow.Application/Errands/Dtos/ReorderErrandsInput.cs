namespace ErrandFlow.Application.Errands.Dtos;

public sealed record ReorderErrandsInput(
    IReadOnlyList<Guid> ErrandIds,
    ulong ExpectedRevision);
