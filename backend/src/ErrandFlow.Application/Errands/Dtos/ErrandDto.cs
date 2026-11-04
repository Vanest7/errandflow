namespace ErrandFlow.Application.Errands.Dtos;

public sealed record ErrandDto(
    Guid Id,
    Guid PlanId,
    Guid LocationId,
    string Title,
    ushort DurationMinutes,
    uint OriginalPosition,
    DateTime? EarliestStartAt,
    DateTime? LatestFinishAt,
    DateTime? FixedStartAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);