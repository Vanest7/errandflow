using ErrandFlow.Application.Planning.Dtos;
using ErrandFlow.Domain.Planning;

namespace ErrandFlow.Application.Planning;

public static class RouteStopBuilder
{
    public static IReadOnlyList<RoutingLocation> Build(
        Guid originLocationId,
        Guid? destinationLocationId,
        ScheduleEvaluation evaluation,
        IReadOnlyList<RoutingLocation> locations)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        ArgumentNullException.ThrowIfNull(locations);

        if (originLocationId == Guid.Empty)
        {
            throw new ArgumentException(
                "An origin location is required.",
                nameof(originLocationId));
        }

        if (destinationLocationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Destination location ID cannot be empty.",
                nameof(destinationLocationId));
        }

        if (!evaluation.IsFeasible ||
            evaluation.FinishedAtUtc is null)
        {
            throw new ArgumentException(
                "A complete feasible schedule is required.",
                nameof(evaluation));
        }

        var locationsById = new Dictionary<Guid, RoutingLocation>();

        foreach (var location in locations)
        {
            if (location is null ||
                location.LocationId == Guid.Empty ||
                location.Latitude is < -90m or > 90m ||
                location.Longitude is < -180m or > 180m)
            {
                throw new ArgumentException(
                    "Locations must have valid IDs and coordinates.",
                    nameof(locations));
            }

            if (!locationsById.TryAdd(location.LocationId, location))
            {
                throw new ArgumentException(
                    "Location IDs must be unique.",
                    nameof(locations));
            }
        }

        var stops = new List<RoutingLocation>();

        void AddStop(Guid locationId)
        {
            if (!locationsById.TryGetValue(locationId, out var location))
            {
                throw new ArgumentException(
                    "Every route stop must have a matching location.",
                    nameof(locations));
            }

            if (stops.Count > 0 &&
                stops[^1].LocationId == locationId)
            {
                return;
            }

            stops.Add(location);
        }

        AddStop(originLocationId);

        foreach (var errand in evaluation.ScheduledErrands)
        {
            AddStop(errand.LocationId);
        }

        if (destinationLocationId is Guid destinationId)
        {
            AddStop(destinationId);
        }

        return stops.AsReadOnly();
    }
}