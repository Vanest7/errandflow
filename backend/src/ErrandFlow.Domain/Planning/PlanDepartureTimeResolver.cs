using ErrandFlow.Domain.Plans;

namespace ErrandFlow.Application.Planning;

public static class PlanDepartureTimeResolver
{
    public static DateTime ResolveUtc(Plan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var localDeparture = plan.PlanDate.ToDateTime(
            plan.DepartureTime,
            DateTimeKind.Unspecified);

        TimeZoneInfo timeZone;

        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(
                plan.TimeZone);
        }
        catch (TimeZoneNotFoundException exception)
        {
            throw new ArgumentException(
                "The plan time zone is not available.",
                "timeZone",
                exception);
        }
        catch (InvalidTimeZoneException exception)
        {
            throw new ArgumentException(
                "The plan time zone is invalid.",
                "timeZone",
                exception);
        }

        if (timeZone.IsInvalidTime(localDeparture))
        {
            throw new ArgumentException(
                "Departure time does not exist in the plan time zone "
                + "because of a daylight saving time change.",
                "departureTime");
        }

        if (timeZone.IsAmbiguousTime(localDeparture))
        {
            throw new ArgumentException(
                "Departure time occurs twice in the plan time zone. "
                + "Choose an unambiguous departure time.",
                "departureTime");
        }

        return TimeZoneInfo.ConvertTimeToUtc(
            localDeparture,
            timeZone);
    }
}