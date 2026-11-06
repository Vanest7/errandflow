using ErrandFlow.Domain.Errands;

namespace ErrandFlow.Domain.Planning;

public static class ScheduleOptimizer
{
    public const int MaximumErrands = 10;
    public const int DefaultMaxExploredNodes = 100_000;

    public static ScheduleOptimizationResult Optimize(
        Guid? originLocationId,
        Guid? destinationLocationId,
        DateTime departureAtUtc,
        IReadOnlyCollection<Errand> errands,
        LocationGraph graph,
        int maxExploredNodes = DefaultMaxExploredNodes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(errands);
        ArgumentNullException.ThrowIfNull(graph);

        if (errands.Count > MaximumErrands)
        {
            throw new ArgumentException(
                $"Optimization supports at most {MaximumErrands} errands.",
                nameof(errands));
        }

        if (maxExploredNodes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxExploredNodes),
                "The search budget must be greater than zero.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var candidates = errands
            .OrderBy(errand => errand.OriginalPosition)
            .ToArray();

        var original = OriginalOrderEvaluator.Evaluate(
            originLocationId,
            destinationLocationId,
            departureAtUtc,
            candidates,
            graph);

        ScheduleEvaluation? best = original.IsFeasible
            ? original
            : null;

        var used = new bool[candidates.Length];
        var prefix = new List<Errand>(candidates.Length);

        var exploredNodes = 0;
        var budgetExhausted = false;

        var totalServiceMinutes = candidates.Sum(
            errand => (int)errand.DurationMinutes);

        void Search(int remainingServiceMinutes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (exploredNodes >= maxExploredNodes)
            {
                budgetExhausted = true;
                return;
            }

            exploredNodes++;

            var partial = OrderedScheduleEvaluator.Evaluate(
                originLocationId,
                null,
                departureAtUtc,
                prefix,
                graph);

            if (!partial.IsFeasible)
            {
                return;
            }

            var partialFinish = partial.FinishedAtUtc!.Value;

            if (best is not null)
            {
                var availableTime =
                    best.FinishedAtUtc!.Value - partialFinish;

                var remainingServiceTime =
                    TimeSpan.FromMinutes(remainingServiceMinutes);

                if (availableTime <= remainingServiceTime)
                {
                    return;
                }
            }

            if (prefix.Count == candidates.Length)
            {
                var complete = OrderedScheduleEvaluator.Evaluate(
                    originLocationId,
                    destinationLocationId,
                    departureAtUtc,
                    prefix,
                    graph);

                if (complete.IsFeasible &&
                    (best is null ||
                     complete.FinishedAtUtc!.Value <
                     best.FinishedAtUtc!.Value))
                {
                    best = complete;
                }

                return;
            }

            for (var index = 0; index < candidates.Length; index++)
            {
                if (used[index])
                {
                    continue;
                }

                var candidate = candidates[index];

                used[index] = true;
                prefix.Add(candidate);

                Search(
                    remainingServiceMinutes -
                    candidate.DurationMinutes);

                prefix.RemoveAt(prefix.Count - 1);
                used[index] = false;

                if (budgetExhausted)
                {
                    return;
                }
            }
        }

        Search(totalServiceMinutes);

        return new ScheduleOptimizationResult(
            best,
            SearchCompleted: !budgetExhausted,
            ExploredNodes: exploredNodes);
    }
}