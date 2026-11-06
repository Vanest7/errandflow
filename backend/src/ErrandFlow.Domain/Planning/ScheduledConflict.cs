namespace ErrandFlow.Domain.Planning;

public sealed record ScheduleConflict(
    string Code,
    Guid? ErrandId,
    Guid FromLocationId,
    Guid ToLocationId);