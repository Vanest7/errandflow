namespace ErrandFlow.Application.Errands.Dtos;

public sealed record SetErrandScheduleInput(
    ulong ExpectedRevision,
    DateTime? EarliestStartAt,
    DateTime? LatestFinishAt,
    DateTime? FixedStartAt);
