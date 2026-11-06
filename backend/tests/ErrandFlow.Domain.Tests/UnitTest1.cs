using ErrandFlow.Domain.Planning;
using ErrandFlow.Domain.Plans;
using Xunit;

namespace ErrandFlow.Domain.Tests;

public sealed class OriginalOrderEvaluatorTests
{
    private static readonly DateTime DepartureAtUtc =
        new(2026, 11, 6, 8, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(null, null, null, 5, null)]
    [InlineData(3, null, null, 5, null)]
    [InlineData(10, null, null, 10, null)]
    [InlineData(null, null, 10, 10, null)]
    [InlineData(null, null, 5, 5, null)]
    [InlineData(null, null, 4, 5, "fixed_start_missed")]
    [InlineData(null, 15, null, 5, null)]
    [InlineData(null, 14, null, 5, "latest_finish_exceeded")]
    public void Evaluate_RespectsScheduleConstraints(
        int? earliestOffset,
        int? latestOffset,
        int? fixedOffset,
        int expectedStartOffset,
        string? expectedConflict)
    {

        var plan = CreatePlan();

        var origin = plan.CreateLocation(
            "Home", null, 39.4700m, -0.3800m);

        var pharmacy = plan.CreateLocation(
            "Pharmacy", null, 39.4710m, -0.3810m);

        var errand = plan.AddErrand(
            pharmacy, "Collect medication", 10);

        plan.SetErrandSchedule(
            errand.Id,
            AtOffset(earliestOffset),
            AtOffset(latestOffset),
            AtOffset(fixedOffset));

        var graph = CreateGraph(origin.Id, pharmacy.Id);

        var result = OriginalOrderEvaluator.Evaluate(
            origin.Id,
            null,
            DepartureAtUtc,
            plan.Errands,
            graph);

        if (expectedConflict is not null)
        {
            Assert.False(result.IsFeasible);
            Assert.Null(result.FinishedAtUtc);
            Assert.Empty(result.ScheduledErrands);

            var conflict = Assert.IsType<ScheduleConflict>(
                result.Conflict);

            Assert.Equal(expectedConflict, conflict.Code);
            Assert.Equal(errand.Id, conflict.ErrandId);
            Assert.Equal(origin.Id, conflict.FromLocationId);
            Assert.Equal(pharmacy.Id, conflict.ToLocationId);
            return;
        }

        Assert.True(result.IsFeasible);
        Assert.Null(result.Conflict);

        var scheduled = Assert.Single(result.ScheduledErrands);

        Assert.Equal(errand.Id, scheduled.ErrandId);
        Assert.Equal(
            DepartureAtUtc.AddMinutes(5),
            scheduled.ArrivalAtUtc);
        Assert.Equal(
            DepartureAtUtc.AddMinutes(expectedStartOffset),
            scheduled.StartAtUtc);
        Assert.Equal(
            DepartureAtUtc.AddMinutes(expectedStartOffset + 10),
            scheduled.FinishAtUtc);
        Assert.Equal(
            TimeSpan.FromMinutes(5),
            scheduled.TravelDuration);
        Assert.Equal(
            TimeSpan.FromMinutes(expectedStartOffset - 5),
            scheduled.WaitingDuration);
        Assert.Equal(
            scheduled.FinishAtUtc,
            result.FinishedAtUtc);
    }

    [Fact]
    public void Evaluate_IncludesReturnTripWithDifferentDuration()
    {
        var plan = CreatePlan();

        var origin = plan.CreateLocation(
            "Home", null, 39.4700m, -0.3800m);

        var pharmacy = plan.CreateLocation(
            "Pharmacy", null, 39.4710m, -0.3810m);

        plan.AddErrand(pharmacy, "Collect medication", 10);

        var graph = CreateGraph(origin.Id, pharmacy.Id);

        var result = OriginalOrderEvaluator.Evaluate(
            origin.Id,
            origin.Id,
            DepartureAtUtc,
            plan.Errands,
            graph);

        Assert.True(result.IsFeasible);

        var scheduled = Assert.Single(result.ScheduledErrands);

        Assert.Equal(
            DepartureAtUtc.AddMinutes(15),
            scheduled.FinishAtUtc);
        Assert.Equal(
            DepartureAtUtc.AddMinutes(23),
            result.FinishedAtUtc);
    }

    [Fact]
    public void Evaluate_ReportsUnreachableErrand()
    {
        var plan = CreatePlan();

        var origin = plan.CreateLocation(
            "Home", null, 39.4700m, -0.3800m);

        var pharmacy = plan.CreateLocation(
            "Pharmacy", null, 39.4710m, -0.3810m);

        var errand = plan.AddErrand(
            pharmacy, "Collect medication", 10);

        var graph = CreateGraph(
            origin.Id,
            pharmacy.Id,
            outboundReachable: false);

        var result = OriginalOrderEvaluator.Evaluate(
            origin.Id,
            null,
            DepartureAtUtc,
            plan.Errands,
            graph);

        Assert.False(result.IsFeasible);
        Assert.Empty(result.ScheduledErrands);
        Assert.Null(result.FinishedAtUtc);

        var conflict = Assert.IsType<ScheduleConflict>(
            result.Conflict);

        Assert.Equal("unreachable_errand", conflict.Code);
        Assert.Equal(errand.Id, conflict.ErrandId);
    }
    [Fact]
    public void Evaluate_PreservesOriginalOrderForErrandsAtSameLocation()
    {
        var plan = CreatePlan();

        var origin = plan.CreateLocation(
            "Home", null, 39.4700m, -0.3800m);

        var pharmacy = plan.CreateLocation(
            "Pharmacy", null, 39.4710m, -0.3810m);

        var firstErrand = plan.AddErrand(
            pharmacy, "Collect medication", 10);

        var secondErrand = plan.AddErrand(
            pharmacy, "Buy sunscreen", 3);

        var graph = CreateGraph(origin.Id, pharmacy.Id);

        var result = OriginalOrderEvaluator.Evaluate(
            origin.Id,
            null,
            DepartureAtUtc,
            new[] { secondErrand, firstErrand },
            graph);

        Assert.True(result.IsFeasible);
        Assert.Equal(2, result.ScheduledErrands.Count);

        var first = result.ScheduledErrands[0];
        var second = result.ScheduledErrands[1];

        Assert.Equal(firstErrand.Id, first.ErrandId);
        Assert.Equal(secondErrand.Id, second.ErrandId);

        Assert.Equal(
            DepartureAtUtc.AddMinutes(5),
            first.ArrivalAtUtc);
        Assert.Equal(
            DepartureAtUtc.AddMinutes(15),
            first.FinishAtUtc);

        Assert.Equal(TimeSpan.Zero, second.TravelDuration);
        Assert.Equal(TimeSpan.Zero, second.WaitingDuration);
        Assert.Equal(first.FinishAtUtc, second.ArrivalAtUtc);
        Assert.Equal(first.FinishAtUtc, second.StartAtUtc);

        Assert.Equal(
            DepartureAtUtc.AddMinutes(18),
            result.FinishedAtUtc);
    }
[Fact]
public void OrderedEvaluator_PreservesProvidedOrder()
{
    var plan = CreatePlan();

    var origin = plan.CreateLocation(
        "Home", null, 39.4700m, -0.3800m);

    var pharmacy = plan.CreateLocation(
        "Pharmacy", null, 39.4710m, -0.3810m);

    var firstErrand = plan.AddErrand(
        pharmacy, "Collect medication", 10);

    var secondErrand = plan.AddErrand(
        pharmacy, "Buy sunscreen", 3);

    var graph = CreateGraph(origin.Id, pharmacy.Id);

    var result = OrderedScheduleEvaluator.Evaluate(
        origin.Id,
        null,
        DepartureAtUtc,
        new[] { secondErrand, firstErrand },
        graph);

    Assert.True(result.IsFeasible);
    Assert.Equal(2, result.ScheduledErrands.Count);

    var first = result.ScheduledErrands[0];
    var second = result.ScheduledErrands[1];

    Assert.Equal(secondErrand.Id, first.ErrandId);
    Assert.Equal(firstErrand.Id, second.ErrandId);

    Assert.Equal(
        DepartureAtUtc.AddMinutes(8),
        first.FinishAtUtc);

    Assert.Equal(first.FinishAtUtc, second.StartAtUtc);
    Assert.Equal(TimeSpan.Zero, second.TravelDuration);

    Assert.Equal(
        DepartureAtUtc.AddMinutes(18),
        result.FinishedAtUtc);
}
[Theory]
[InlineData(100, true, 15, true)]
[InlineData(1, false, 33, false)]
public void Optimize_RespectsSearchBudgetAndKeepsBestKnownSchedule(
    int searchBudget,
    bool expectedSearchCompleted,
    int expectedFinishOffset,
    bool expectedReversedOrder)
{
    var plan = CreatePlan();

    var origin = plan.CreateLocation(
        "Home", null, 39.4700m, -0.3800m);

    var pharmacy = plan.CreateLocation(
        "Pharmacy", null, 39.4710m, -0.3810m);

    var bakery = plan.CreateLocation(
        "Bakery", null, 39.4720m, -0.3820m);

    var firstErrand = plan.AddErrand(
        pharmacy, "Collect medication", 10);

    var secondErrand = plan.AddErrand(
        bakery, "Buy bread", 3);

    var costs = new Dictionary<(Guid From, Guid To), TravelCost>
    {
        [(origin.Id, pharmacy.Id)] =
            TravelCost.Create(TimeSpan.FromMinutes(10), 800m),
        [(origin.Id, bakery.Id)] =
            TravelCost.Create(TimeSpan.FromMinutes(1), 80m),
        [(pharmacy.Id, origin.Id)] =
            TravelCost.Create(TimeSpan.FromMinutes(1), 80m),
        [(pharmacy.Id, bakery.Id)] =
            TravelCost.Create(TimeSpan.FromMinutes(10), 800m),
        [(bakery.Id, origin.Id)] =
            TravelCost.Create(TimeSpan.FromMinutes(1), 80m),
        [(bakery.Id, pharmacy.Id)] =
            TravelCost.Create(TimeSpan.FromMinutes(1), 80m)
    };

    var graph = new LocationGraph(
        new[] { origin.Id, pharmacy.Id, bakery.Id },
        costs);

    var result = ScheduleOptimizer.Optimize(
        origin.Id,
        null,
        DepartureAtUtc,
        plan.Errands,
        graph,
        maxExploredNodes: searchBudget);

    Assert.Equal(expectedSearchCompleted, result.SearchCompleted);
    Assert.Equal(expectedSearchCompleted, result.IsOptimal);
    Assert.False(result.IsProvenInfeasible);
    Assert.InRange(result.ExploredNodes, 1, searchBudget);

    var best = Assert.IsType<ScheduleEvaluation>(
        result.BestSchedule);

    Assert.True(best.IsFeasible);
    Assert.Equal(
        DepartureAtUtc.AddMinutes(expectedFinishOffset),
        best.FinishedAtUtc);

    var expectedOrder = expectedReversedOrder
        ? new[] { secondErrand.Id, firstErrand.Id }
        : new[] { firstErrand.Id, secondErrand.Id };

    Assert.Equal(
        expectedOrder,
        best.ScheduledErrands.Select(errand => errand.ErrandId));
}
[Theory]
[InlineData(5, 100, true, true)]
[InlineData(-1, 100, true, false)]
[InlineData(5, 1, false, false)]
public void Optimize_DistinguishesFeasibleInfeasibleAndIncompleteSearch(
    int appointmentOffset,
    int searchBudget,
    bool expectedSearchCompleted,
    bool expectedSolution)
{
    var plan = CreatePlan();

    var location = plan.CreateLocation(
        "Service center", null, 39.4700m, -0.3800m);

    var flexibleErrand = plan.AddErrand(
        location, "Collect documents", 10);

    var appointment = plan.AddErrand(
        location, "Attend appointment", 3);

    plan.SetErrandSchedule(
        appointment.Id,
        earliestStartAt: null,
        latestFinishAt: null,
        fixedStartAt: DepartureAtUtc.AddMinutes(appointmentOffset));

    var graph = new LocationGraph(
        new[] { location.Id },
        new Dictionary<(Guid From, Guid To), TravelCost>());

    var original = OriginalOrderEvaluator.Evaluate(
        location.Id,
        null,
        DepartureAtUtc,
        plan.Errands,
        graph);

    Assert.False(original.IsFeasible);

    var result = ScheduleOptimizer.Optimize(
        location.Id,
        null,
        DepartureAtUtc,
        plan.Errands,
        graph,
        maxExploredNodes: searchBudget);

    Assert.Equal(expectedSearchCompleted, result.SearchCompleted);
    Assert.Equal(
        expectedSearchCompleted && expectedSolution,
        result.IsOptimal);
    Assert.Equal(
        expectedSearchCompleted && !expectedSolution,
        result.IsProvenInfeasible);

    if (!expectedSolution)
    {
        Assert.Null(result.BestSchedule);
        return;
    }

    var best = Assert.IsType<ScheduleEvaluation>(
        result.BestSchedule);

    Assert.True(best.IsFeasible);

    Assert.Equal(
        new[] { appointment.Id, flexibleErrand.Id },
        best.ScheduledErrands.Select(errand => errand.ErrandId));

    Assert.Equal(
        TimeSpan.FromMinutes(5),
        best.ScheduledErrands[0].WaitingDuration);

    Assert.Equal(
        DepartureAtUtc.AddMinutes(18),
        best.FinishedAtUtc);
}
[Fact]
public void Optimize_ChoosesOrderUsingFinalDestination()
{
    var plan = CreatePlan();

    var home = plan.CreateLocation(
        "Home", null, 39.4700m, -0.3800m);

    var pharmacy = plan.CreateLocation(
        "Pharmacy", null, 39.4710m, -0.3810m);

    var bakery = plan.CreateLocation(
        "Bakery", null, 39.4720m, -0.3820m);

    var pharmacyErrand = plan.AddErrand(
        pharmacy, "Collect medication", 10);

    var bakeryErrand = plan.AddErrand(
        bakery, "Buy bread", 3);

    var costs = new Dictionary<(Guid From, Guid To), TravelCost>
    {
        [(home.Id, pharmacy.Id)] =
            TravelCost.Create(TimeSpan.FromMinutes(1), 80m),
        [(home.Id, bakery.Id)] =
            TravelCost.Create(TimeSpan.FromMinutes(2), 160m),
        [(pharmacy.Id, home.Id)] =
            TravelCost.Create(TimeSpan.FromMinutes(1), 80m),
        [(pharmacy.Id, bakery.Id)] =
            TravelCost.Create(TimeSpan.FromMinutes(1), 80m),
        [(bakery.Id, home.Id)] =
            TravelCost.Create(TimeSpan.FromMinutes(2), 160m),
        [(bakery.Id, pharmacy.Id)] =
            TravelCost.Create(TimeSpan.FromSeconds(30), 40m)
    };

    var graph = new LocationGraph(
        new[] { home.Id, pharmacy.Id, bakery.Id },
        costs);

    var withoutDestination = ScheduleOptimizer.Optimize(
        home.Id,
        null,
        DepartureAtUtc,
        plan.Errands,
        graph);

    var returningHome = ScheduleOptimizer.Optimize(
        home.Id,
        home.Id,
        DepartureAtUtc,
        plan.Errands,
        graph);

    Assert.True(withoutDestination.IsOptimal);
    Assert.True(returningHome.IsOptimal);

    var outbound = Assert.IsType<ScheduleEvaluation>(
        withoutDestination.BestSchedule);

    var roundTrip = Assert.IsType<ScheduleEvaluation>(
        returningHome.BestSchedule);

    Assert.Equal(
        new[] { pharmacyErrand.Id, bakeryErrand.Id },
        outbound.ScheduledErrands.Select(errand => errand.ErrandId));

    Assert.Equal(
        DepartureAtUtc.AddMinutes(15),
        outbound.FinishedAtUtc);

    Assert.Equal(
        new[] { bakeryErrand.Id, pharmacyErrand.Id },
        roundTrip.ScheduledErrands.Select(errand => errand.ErrandId));

    Assert.Equal(
        DepartureAtUtc.AddMinutes(16).AddSeconds(30),
        roundTrip.FinishedAtUtc);
}
[Fact]
public void Optimize_RejectsMoreThanTenErrands()
{
    var plan = CreatePlan();

    var location = plan.CreateLocation(
        "Service center", null, 39.4700m, -0.3800m);

    for (var index = 0; index < 11; index++)
    {
        plan.AddErrand(
            location,
            $"Task {index + 1}",
            1);
    }

    var graph = new LocationGraph(
        new[] { location.Id },
        new Dictionary<(Guid From, Guid To), TravelCost>());

    var exception = Assert.Throws<ArgumentException>(() =>
        ScheduleOptimizer.Optimize(
            location.Id,
            null,
            DepartureAtUtc,
            plan.Errands,
            graph));

    Assert.Equal("errands", exception.ParamName);
}
    [Fact]
    public void Evaluate_PreservesCompletedErrandsWhenDestinationIsUnreachable()
    {
        var plan = CreatePlan();

        var origin = plan.CreateLocation(
            "Home", null, 39.4700m, -0.3800m);

        var pharmacy = plan.CreateLocation(
            "Pharmacy", null, 39.4710m, -0.3810m);

        var errand = plan.AddErrand(
            pharmacy, "Collect medication", 10);

        var costs = new Dictionary<(Guid From, Guid To), TravelCost>
        {
            [(origin.Id, pharmacy.Id)] =
                TravelCost.Create(TimeSpan.FromMinutes(5), 400m),
            [(pharmacy.Id, origin.Id)] = TravelCost.Unreachable
        };

        var graph = new LocationGraph(
            new[] { origin.Id, pharmacy.Id },
            costs);

        var result = OriginalOrderEvaluator.Evaluate(
            origin.Id,
            origin.Id,
            DepartureAtUtc,
            plan.Errands,
            graph);

        Assert.False(result.IsFeasible);
        Assert.Null(result.FinishedAtUtc);

        var scheduled = Assert.Single(result.ScheduledErrands);

        Assert.Equal(errand.Id, scheduled.ErrandId);
        Assert.Equal(
            DepartureAtUtc.AddMinutes(15),
            scheduled.FinishAtUtc);

        var conflict = Assert.IsType<ScheduleConflict>(
            result.Conflict);

        Assert.Equal("unreachable_destination", conflict.Code);
        Assert.Null(conflict.ErrandId);
        Assert.Equal(pharmacy.Id, conflict.FromLocationId);
        Assert.Equal(origin.Id, conflict.ToLocationId);
    }

    [Fact]
    public void Evaluate_WithoutErrandsOrDestinationFinishesAtDeparture()
    {
        var plan = CreatePlan();

        var origin = plan.CreateLocation(
            "Home", null, 39.4700m, -0.3800m);

        var graph = new LocationGraph(
            new[] { origin.Id },
            new Dictionary<(Guid From, Guid To), TravelCost>());

        var result = OriginalOrderEvaluator.Evaluate(
            origin.Id,
            null,
            DepartureAtUtc,
            plan.Errands,
            graph);

        Assert.True(result.IsFeasible);
        Assert.Null(result.Conflict);
        Assert.Empty(result.ScheduledErrands);
        Assert.Equal(DepartureAtUtc, result.FinishedAtUtc);
    }

    
    private static Plan CreatePlan()
    {
        return new Plan(
            Guid.NewGuid(),
            "Morning errands",
            new DateOnly(2026, 11, 6),
            new TimeOnly(8, 0),
            "UTC");
    }

    private static DateTime? AtOffset(int? minutes)
    {
        return minutes.HasValue
            ? DepartureAtUtc.AddMinutes(minutes.Value)
            : null;
    }

    private static LocationGraph CreateGraph(
        Guid originId,
        Guid pharmacyId,
        bool outboundReachable = true)
    {
        var costs =
            new Dictionary<(Guid From, Guid To), TravelCost>
            {
                [(originId, pharmacyId)] = outboundReachable
                    ? TravelCost.Create(TimeSpan.FromMinutes(5), 400m)
                    : TravelCost.Unreachable,
                [(pharmacyId, originId)] =
                    TravelCost.Create(TimeSpan.FromMinutes(8), 600m)
            };

        return new LocationGraph(
            new[] { originId, pharmacyId },
            costs);
    }
}