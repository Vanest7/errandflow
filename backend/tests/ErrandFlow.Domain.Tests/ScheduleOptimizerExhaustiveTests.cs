using ErrandFlow.Domain.Errands;
using ErrandFlow.Domain.Planning;
using ErrandFlow.Domain.Plans;
using Xunit;

namespace ErrandFlow.Domain.Tests;

public sealed class ScheduleOptimizerExhaustiveTests
{
    private static readonly DateTime DepartureAtUtc =
        new(2026, 11, 6, 8, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 20)]
    [InlineData(3, 30)]
    [InlineData(4, 40)]
    [InlineData(5, 50)]
    [InlineData(5, 60)]
    public void Optimize_MatchesExhaustiveSearch(
        int errandCount,
        int seed)
    {
        var random = new Random(seed);

        var plan = new Plan(
            Guid.NewGuid(),
            "Optimization comparison",
            new DateOnly(2026, 11, 6),
            new TimeOnly(8, 0),
            "UTC");

        var origin = plan.CreateLocation(
            "Home", null, 39.4700m, -0.3800m);

        var locationIds = new List<Guid> { origin.Id };

        for (var index = 0; index < errandCount; index++)
        {
            var location = plan.CreateLocation(
                $"Location {index + 1}",
                null,
                39.4710m,
                -0.3810m);

            locationIds.Add(location.Id);

            var errand = plan.AddErrand(
                location,
                $"Task {index + 1}",
                (ushort)random.Next(1, 11));

            if (index % 3 == 0)
            {
                plan.SetErrandSchedule(
                    errand.Id,
                    earliestStartAt: DepartureAtUtc.AddMinutes(10),
                    latestFinishAt: DepartureAtUtc.AddMinutes(60),
                    fixedStartAt: null);
            }
            else if (index % 3 == 1)
            {
                plan.SetErrandSchedule(
                    errand.Id,
                    earliestStartAt: null,
                    latestFinishAt: null,
                    fixedStartAt: DepartureAtUtc.AddMinutes(25));
            }
        }

        var costs =
            new Dictionary<(Guid From, Guid To), TravelCost>();

        foreach (var from in locationIds)
        {
            foreach (var to in locationIds)
            {
                if (from == to)
                {
                    continue;
                }

                costs[(from, to)] = random.Next(10) == 0
                    ? TravelCost.Unreachable
                    : TravelCost.Create(
                        TimeSpan.FromMinutes(random.Next(1, 16)),
                        100m);
            }
        }

        var graph = new LocationGraph(locationIds, costs);
        var errands = plan.Errands.ToArray();

        DateTime? exhaustiveBestFinish = null;
        var evaluatedOrders = 0;

        foreach (var order in GetPermutations(errands))
        {
            evaluatedOrders++;

            var evaluation = OrderedScheduleEvaluator.Evaluate(
                origin.Id,
                origin.Id,
                DepartureAtUtc,
                order,
                graph);

            if (!evaluation.IsFeasible)
            {
                continue;
            }

            var finish = evaluation.FinishedAtUtc!.Value;

            if (exhaustiveBestFinish is null ||
                finish < exhaustiveBestFinish.Value)
            {
                exhaustiveBestFinish = finish;
            }
        }

        var expectedOrderCount = 1;

        for (var value = 2; value <= errandCount; value++)
        {
            expectedOrderCount *= value;
        }

        Assert.Equal(expectedOrderCount, evaluatedOrders);

        var result = ScheduleOptimizer.Optimize(
            origin.Id,
            origin.Id,
            DepartureAtUtc,
            errands,
            graph,
            maxExploredNodes: 10_000);

        Assert.True(result.SearchCompleted);

        if (exhaustiveBestFinish is null)
        {
            Assert.Null(result.BestSchedule);
            Assert.True(result.IsProvenInfeasible);
            Assert.False(result.IsOptimal);
            return;
        }

        var best = Assert.IsType<ScheduleEvaluation>(
            result.BestSchedule);

        Assert.True(result.IsOptimal);
        Assert.False(result.IsProvenInfeasible);
        Assert.True(best.IsFeasible);
        Assert.Equal(exhaustiveBestFinish, best.FinishedAtUtc);

        var returnedIds = best.ScheduledErrands
            .Select(errand => errand.ErrandId)
            .ToArray();

        Assert.Equal(errands.Length, returnedIds.Length);
        Assert.Equal(
            errands.Length,
            returnedIds.Distinct().Count());

        Assert.True(
            errands.Select(errand => errand.Id)
                .ToHashSet()
                .SetEquals(returnedIds));

        var errandsById = errands.ToDictionary(errand => errand.Id);

        var returnedOrder = returnedIds
            .Select(id => errandsById[id])
            .ToArray();

        var verified = OrderedScheduleEvaluator.Evaluate(
            origin.Id,
            origin.Id,
            DepartureAtUtc,
            returnedOrder,
            graph);

        Assert.True(verified.IsFeasible);
        Assert.Equal(best.FinishedAtUtc, verified.FinishedAtUtc);

        Assert.Equal(
            best.ScheduledErrands.ToArray(),
            verified.ScheduledErrands.ToArray());
    }

    private static IEnumerable<Errand[]> GetPermutations(
        Errand[] errands)
    {
        var current = new Errand[errands.Length];
        var used = new bool[errands.Length];

        IEnumerable<Errand[]> Generate(int depth)
        {
            if (depth == errands.Length)
            {
                yield return current.ToArray();
                yield break;
            }

            for (var index = 0; index < errands.Length; index++)
            {
                if (used[index])
                {
                    continue;
                }

                used[index] = true;
                current[depth] = errands[index];

                foreach (var permutation in Generate(depth + 1))
                {
                    yield return permutation;
                }

                used[index] = false;
            }
        }

        return Generate(0);
    }
}