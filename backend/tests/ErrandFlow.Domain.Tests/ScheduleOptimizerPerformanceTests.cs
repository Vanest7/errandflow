using System.Diagnostics;
using ErrandFlow.Domain.Planning;
using ErrandFlow.Domain.Plans;
using Xunit;
using Xunit.Abstractions;

namespace ErrandFlow.Domain.Tests;

public sealed class ScheduleOptimizerPerformanceTests
{
    private readonly ITestOutputHelper _output;

    public ScheduleOptimizerPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData(101, false)]
    [InlineData(202, false)]
    [InlineData(303, true)]
    public void Optimize_ReportsSearchMetricsForTenErrands(
        int seed,
        bool includeOpeningTimes)
    {
        var departureAtUtc =
            new DateTime(2026, 11, 6, 8, 0, 0, DateTimeKind.Utc);

        var random = new Random(seed);

        var plan = new Plan(
            Guid.NewGuid(),
            "Ten errands",
            new DateOnly(2026, 11, 6),
            new TimeOnly(8, 0),
            "UTC");

        var origin = plan.CreateLocation(
            "Home", null, 39.4700m, -0.3800m);

        var locationIds = new List<Guid> { origin.Id };

        for (var index = 0; index < 10; index++)
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
                (ushort)random.Next(2, 16));

            if (includeOpeningTimes && index % 2 == 0)
            {
                plan.SetErrandSchedule(
                    errand.Id,
                    earliestStartAt: departureAtUtc.AddMinutes(
                        random.Next(30, 121)),
                    latestFinishAt: null,
                    fixedStartAt: null);
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

                costs[(from, to)] = TravelCost.Create(
                    TimeSpan.FromMinutes(random.Next(1, 21)),
                    100m);
            }
        }

        var graph = new LocationGraph(locationIds, costs);

        var original = OriginalOrderEvaluator.Evaluate(
            origin.Id,
            origin.Id,
            departureAtUtc,
            plan.Errands,
            graph);

        Assert.True(original.IsFeasible);

        var stopwatch = Stopwatch.StartNew();

        var result = ScheduleOptimizer.Optimize(
            origin.Id,
            origin.Id,
            departureAtUtc,
            plan.Errands,
            graph);

        stopwatch.Stop();

        var best = Assert.IsType<ScheduleEvaluation>(
            result.BestSchedule);

        Assert.True(best.IsFeasible);
        Assert.Equal(10, best.ScheduledErrands.Count);

        Assert.True(
            best.FinishedAtUtc!.Value <=
            original.FinishedAtUtc!.Value);

        Assert.InRange(
            result.ExploredNodes,
            1,
            ScheduleOptimizer.DefaultMaxExploredNodes);

        var originalDuration =
            original.FinishedAtUtc!.Value - departureAtUtc;

        var bestDuration =
            best.FinishedAtUtc!.Value - departureAtUtc;

        _output.WriteLine($"Seed: {seed}");
        _output.WriteLine($"Opening times: {includeOpeningTimes}");
        _output.WriteLine(
            $"Elapsed milliseconds: {stopwatch.Elapsed.TotalMilliseconds:F2}");
        _output.WriteLine($"Explored nodes: {result.ExploredNodes}");
        _output.WriteLine($"Search completed: {result.SearchCompleted}");
        _output.WriteLine($"Optimality proven: {result.IsOptimal}");
        _output.WriteLine(
            $"Original duration minutes: {originalDuration.TotalMinutes:F2}");
        _output.WriteLine(
            $"Best duration minutes: {bestDuration.TotalMinutes:F2}");
    }
}