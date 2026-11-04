namespace ErrandFlow.Application.Errands.Dtos;

public sealed record CreateErrandInput(
    Guid LocationId,
    string Title,
    ushort DurationMinutes,
    ulong ExpectedRevision);