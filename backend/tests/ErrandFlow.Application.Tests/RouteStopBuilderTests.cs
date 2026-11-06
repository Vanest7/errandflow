using ErrandFlow.Application.Planning;
using ErrandFlow.Application.Planning.Dtos;
using ErrandFlow.Domain.Planning;
using Xunit;

namespace ErrandFlow.Application.Tests;

public sealed class RouteStopBuilderTests
{
    private static readonly DateTime DepartureAtUtc =
        new(2026, 11, 6, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Build_CollapsesConsecutiveDuplicatesButPreservesReturnVisit()
    {
        var home = CreateLocation();
        var pharmacy = CreateLocation();

        var schedule = CreateSchedule(
            home.LocationId,
            home.LocationId,
            pharmacy.LocationId,
            pharmacy.LocationId,
            home.LocationId);

        var stops = RouteStopBuilder.Build(
            home.LocationId,
            home.LocationId,
            schedule,
            new[] { home, pharmacy });

        Assert.Equal(
            new[]
            {
                home.LocationId,
                pharmacy.LocationId,
                home.LocationId
            },
            stops.Select(stop => stop.LocationId));

        Assert.Equal(5, schedule.ScheduledErrands.Count);
    }

    [Fact]
    public void Build_IncludesDestinationWhenThereAreNoErrands()
    {
        var home = CreateLocation();
        var destination = CreateLocation();

        var stops = RouteStopBuilder.Build(
            home.LocationId,
            destination.LocationId,
            CreateSchedule(),
            new[] { home, destination });

        Assert.Equal(
            new[] { home.LocationId, destination.LocationId },
            stops.Select(stop => stop.LocationId));
    }

    [Fact]
    public void Build_RejectsMissingLocation()
    {
        var home = CreateLocation();
        var missingLocationId = Guid.NewGuid();

        var exception = Assert.Throws<ArgumentException>(() =>
            RouteStopBuilder.Build(
                home.LocationId,
                null,
                CreateSchedule(missingLocationId),
                new[] { home }));

        Assert.Equal("locations", exception.ParamName);
    }

    [Fact]
    public void Build_RejectsInfeasibleSchedule()
    {
        var home = CreateLocation();

        var schedule = new ScheduleEvaluation(
            Array.Empty<ScheduledErrand>(),
            null,
            new ScheduleConflict(
                "fixed_start_missed",
                Guid.NewGuid(),
                home.LocationId,
                home.LocationId));

        var exception = Assert.Throws<ArgumentException>(() =>
            RouteStopBuilder.Build(
                home.LocationId,
                null,
                schedule,
                new[] { home }));

        Assert.Equal("evaluation", exception.ParamName);
    }

    private static RoutingLocation CreateLocation()
    {
        return new RoutingLocation(
            Guid.NewGuid(),
            39.4700m,
            -0.3800m);
    }

    private static ScheduleEvaluation CreateSchedule(
        params Guid[] locationIds)
    {
        var errands = locationIds
            .Select((locationId, index) =>
            {
                var start = DepartureAtUtc.AddMinutes(index);

                return new ScheduledErrand(
                    Guid.NewGuid(),
                    locationId,
                    start,
                    start,
                    start.AddMinutes(1),
                    TimeSpan.Zero,
                    TimeSpan.Zero);
            })
            .ToArray();

        return new ScheduleEvaluation(
            errands,
            DepartureAtUtc.AddMinutes(errands.Length),
            null);
    }
}