using ErrandFlow.Domain.Errands;

namespace ErrandFlow.Domain.Planning;

public static class OriginalOrderEvaluator
{
    public static ScheduleEvaluation Evaluate(
        Guid? originLocationId,
        Guid? destinationLocationId,
        DateTime departureAtUtc,
        IReadOnlyCollection<Errand> errands,
        LocationGraph graph)
    {
        ArgumentNullException.ThrowIfNull(errands);

        var orderedErrands = errands
            .OrderBy(errand => errand.OriginalPosition)
            .ToArray();

        return OrderedScheduleEvaluator.Evaluate(
            originLocationId,
            destinationLocationId,
            departureAtUtc,
            orderedErrands,
            graph);
    }
}