namespace ErrandFlow.Domain.Planning;

public sealed record ScheduledErrand(
    Guid ErrandId,
    Guid LocationId,
    DateTime ArrivalAtUtc,
    DateTime StartAtUtc,
    DateTime FinishAtUtc,
    TimeSpan TravelDuration,
    TimeSpan WaitingDuration);