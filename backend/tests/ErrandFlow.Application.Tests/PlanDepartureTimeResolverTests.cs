using System.Globalization;
using ErrandFlow.Application.Planning;
using ErrandFlow.Domain.Plans;
using Xunit;

namespace ErrandFlow.Application.Tests;

public sealed class PlanDepartureTimeResolverTests
{
    [Theory]
    [InlineData(
        "2026-01-15",
        "09:00",
        "Europe/Madrid",
        "2026-01-15T08:00:00Z")]
    [InlineData(
        "2026-07-15",
        "09:00",
        "Europe/Madrid",
        "2026-07-15T07:00:00Z")]
    [InlineData(
        "2026-01-15",
        "00:30",
        "Europe/Madrid",
        "2026-01-14T23:30:00Z")]
    [InlineData(
        "2026-07-15",
        "09:00",
        "UTC",
        "2026-07-15T09:00:00Z")]
    public void ResolveUtc_UsesPlanTimeZone(
        string date,
        string time,
        string timeZone,
        string expectedUtc)
    {
        var plan = CreatePlan(date, time, timeZone);

        var expected = DateTimeOffset.Parse(
            expectedUtc,
            CultureInfo.InvariantCulture).UtcDateTime;

        var actual = PlanDepartureTimeResolver.ResolveUtc(plan);

        Assert.Equal(expected, actual);
        Assert.Equal(DateTimeKind.Utc, actual.Kind);
    }

    [Theory]
    [InlineData("2026-03-29", "02:30")]
    [InlineData("2026-10-25", "02:30")]
    public void ResolveUtc_RejectsInvalidOrAmbiguousDeparture(
        string date,
        string time)
    {
        var plan = CreatePlan(
            date,
            time,
            "Europe/Madrid");

        var exception = Assert.Throws<ArgumentException>(() =>
            PlanDepartureTimeResolver.ResolveUtc(plan));

        Assert.Equal("departureTime", exception.ParamName);
    }

    private static Plan CreatePlan(
        string date,
        string time,
        string timeZone)
    {
        var planDate = DateOnly.ParseExact(
            date,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture);

        var departureTime = TimeOnly.ParseExact(
            time,
            "HH:mm",
            CultureInfo.InvariantCulture);

        return new Plan(
            Guid.NewGuid(),
            "Departure conversion",
            planDate,
            departureTime,
            timeZone);
    }
}