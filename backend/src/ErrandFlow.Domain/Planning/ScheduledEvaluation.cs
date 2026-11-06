namespace ErrandFlow.Domain.Planning;

public sealed record ScheduleEvaluation(
    IReadOnlyList<ScheduledErrand> ScheduledErrands,
    DateTime? FinishedAtUtc,
    ScheduleConflict? Conflict)
{
    public bool IsFeasible => Conflict is null;
}