using ErrandFlow.Application.Planning;
using ErrandFlow.Application.Planning.Dtos;
using ErrandFlow.Domain.Planning;
using ErrandFlow.Domain.Plans;
using Xunit;

namespace ErrandFlow.Application.Tests;

public sealed class RouteScheduleEvaluatorTests
{
    private static readonly DateTime DepartureAtUtc =
        new(2026, 11, 6, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Evaluate_PreservesFractionalTravelDuration()
    {
        var plan = CreatePlan();

        var home = plan.CreateLocation(
            "Home", null, 39.4700m, -0.3800m);

        var pharmacy = plan.CreateLocation(
            "Pharmacy", null, 39.4710m, -0.3810m);

        var errand = plan.AddErrand(
            pharmacy, "Collect medication", 10);

        var path = CreatePath(
            home.Id,
            pharmacy.Id,
            320.514d);

        var result = RouteScheduleEvaluator.Evaluate(
            home.Id,
            null,
            DepartureAtUtc,
            new[] { errand },
            path);

        Assert.True(result.IsFeasible);

        var scheduled = Assert.Single(result.ScheduledErrands);
        var expectedTravel = TimeSpan.FromSeconds(320.514d);

        Assert.Equal(expectedTravel, scheduled.TravelDuration);
        Assert.Equal(
            DepartureAtUtc.Add(expectedTravel),
            scheduled.ArrivalAtUtc);
        Assert.Equal(
            DepartureAtUtc.Add(expectedTravel).AddMinutes(10),
            result.FinishedAtUtc);
    }

    [Fact]
    public void Evaluate_DetectsAppointmentMissedWithRouteDuration()
    {
        var plan = CreatePlan();

        var home = plan.CreateLocation(
            "Home", null, 39.4700m, -0.3800m);

        var pharmacy = plan.CreateLocation(
            "Pharmacy", null, 39.4710m, -0.3810m);

        var errand = plan.AddErrand(
            pharmacy, "Attend appointment", 10);

        plan.SetErrandSchedule(
            errand.Id,
            earliestStartAt: null,
            latestFinishAt: null,
            fixedStartAt: DepartureAtUtc.AddSeconds(320));

        var matrixGraph = new LocationGraph(
            new[] { home.Id, pharmacy.Id },
            new Dictionary<(Guid From, Guid To), TravelCost>
            {
                [(home.Id, pharmacy.Id)] =
                    TravelCost.Create(TimeSpan.FromSeconds(320), 424m),
                [(pharmacy.Id, home.Id)] =
                    TravelCost.Unreachable
            });

        var matrixEvaluation = OrderedScheduleEvaluator.Evaluate(
            home.Id,
            null,
            DepartureAtUtc,
            new[] { errand },
            matrixGraph);

        Assert.True(matrixEvaluation.IsFeasible);

        var routeEvaluation = RouteScheduleEvaluator.Evaluate(
            home.Id,
            null,
            DepartureAtUtc,
            new[] { errand },
            CreatePath(home.Id, pharmacy.Id, 320.514d));

        Assert.False(routeEvaluation.IsFeasible);
        Assert.Null(routeEvaluation.FinishedAtUtc);

        var conflict = Assert.IsType<ScheduleConflict>(
            routeEvaluation.Conflict);

        Assert.Equal("fixed_start_missed", conflict.Code);
        Assert.Equal(errand.Id, conflict.ErrandId);
    }

    [Fact]
    public void Evaluate_IncludesFinalTravelAfterSameLocationErrand()
    {
        var plan = CreatePlan();

        var home = plan.CreateLocation(
            "Home", null, 39.4700m, -0.3800m);

        var destination = plan.CreateLocation(
            "Destination", null, 39.4710m, -0.3810m);

        var errand = plan.AddErrand(
            home, "Collect documents", 10);

        var result = RouteScheduleEvaluator.Evaluate(
            home.Id,
            destination.Id,
            DepartureAtUtc,
            new[] { errand },
            CreatePath(home.Id, destination.Id, 90d));

        Assert.True(result.IsFeasible);

        var scheduled = Assert.Single(result.ScheduledErrands);

        Assert.Equal(TimeSpan.Zero, scheduled.TravelDuration);
        Assert.Equal(
            DepartureAtUtc.AddMinutes(10),
            scheduled.FinishAtUtc);
        Assert.Equal(
            DepartureAtUtc.AddMinutes(11).AddSeconds(30),
            result.FinishedAtUtc);
    }

    private static Plan CreatePlan()
    {
        return new Plan(
            Guid.NewGuid(),
            "Route validation",
            new DateOnly(2026, 11, 6),
            new TimeOnly(9, 0),
            "UTC");
    }

    private static RoutePathDto CreatePath(
        Guid from,
        Guid to,
        double seconds)
    {
        return new RoutePathDto(
            new[]
            {
                new RouteLegDto(
                    from,
                    to,
                    424d,
                    seconds,
                    Array.Empty<RouteCoordinate>())
            });
    }
}