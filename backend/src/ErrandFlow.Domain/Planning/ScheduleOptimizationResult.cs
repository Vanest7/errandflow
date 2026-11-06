namespace ErrandFlow.Domain.Planning;

public sealed record ScheduleOptimizationResult(
    ScheduleEvaluation? BestSchedule,
    bool SearchCompleted,
    int ExploredNodes)
{
    public bool IsOptimal =>
        SearchCompleted && BestSchedule is not null;

    public bool IsProvenInfeasible =>
        SearchCompleted && BestSchedule is null;
}