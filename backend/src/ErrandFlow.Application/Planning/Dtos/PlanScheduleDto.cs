namespace ErrandFlow.Application.Planning.Dtos;

public sealed record PlanScheduleDto(
    Guid PlanId,
    ulong Revision,
    string TravelMode,
    DateTime DepartureAtUtc,
    bool IsFeasible,
    DateTime? FinishedAtUtc,
    IReadOnlyList<ScheduledErrandDto> ScheduledErrands,
    ScheduleConflictDto? Conflict);

public sealed record ScheduledErrandDto(
    Guid ErrandId,
    Guid LocationId,
    DateTime ArrivalAtUtc,
    DateTime StartAtUtc,
    DateTime FinishAtUtc,
    double TravelDurationSeconds,
    double WaitingDurationSeconds);

public sealed record ScheduleConflictDto(
    string Code,
    Guid? ErrandId,
    Guid FromLocationId,
    Guid ToLocationId);