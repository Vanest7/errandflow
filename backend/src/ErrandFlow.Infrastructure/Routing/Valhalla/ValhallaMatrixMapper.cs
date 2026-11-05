using ErrandFlow.Application.Planning.Dtos;
using ErrandFlow.Application.Planning.Exceptions;
using ErrandFlow.Domain.Planning;

namespace ErrandFlow.Infrastructure.Routing.Valhalla;

internal static class ValhallaMatrixMapper
{
    public static LocationGraph ToGraph(
        ValhallaMatrixResponse response,
        IReadOnlyList<RoutingLocation> locations)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(locations);

        var locationIds = locations
            .Select(location => location.LocationId)
            .ToArray();

        if (locationIds.Any(id => id == Guid.Empty) ||
            locationIds.Distinct().Count() != locationIds.Length)
        {
            throw new ArgumentException(
                "Locations must have unique, non-empty IDs.",
                nameof(locations));
        }

        var rows = response.SourcesToTargets;

        if (response.Units != "kilometers" ||
            rows is null ||
            rows.Count != locationIds.Length)
        {
            throw new TravelGraphUnavailableException();
        }

        var costs =
            new Dictionary<(Guid From, Guid To), TravelCost>();

        for (var fromIndex = 0; fromIndex < locationIds.Length; fromIndex++)
        {
            var row = rows[fromIndex];

            if (row is null || row.Count != locationIds.Length)
            {
                throw new TravelGraphUnavailableException();
            }

            for (var toIndex = 0; toIndex < locationIds.Length; toIndex++)
            {
                var cell = row[toIndex];

                if (cell is null ||
                    cell.FromIndex != fromIndex ||
                    cell.ToIndex != toIndex)
                {
                    throw new TravelGraphUnavailableException();
                }

                if (fromIndex == toIndex)
                {
                    if (cell.TimeSeconds != 0m || cell.Distance != 0m)
                    {
                        throw new TravelGraphUnavailableException();
                    }

                    continue;
                }

                costs.Add(
                    (locationIds[fromIndex], locationIds[toIndex]),
                    ToTravelCost(cell));
            }
        }

        return new LocationGraph(locationIds, costs);
    }

    private static TravelCost ToTravelCost(ValhallaMatrixCell cell)
    {
        if (cell.TimeSeconds is null && cell.Distance is null)
        {
            return TravelCost.Unreachable;
        }

        if (cell.TimeSeconds is not decimal seconds ||
            cell.Distance is not decimal kilometers)
        {
            throw new TravelGraphUnavailableException();
        }

        var maximumSeconds =
            (decimal)long.MaxValue / TimeSpan.TicksPerSecond;

        if (seconds < 0m ||
            kilometers < 0m ||
            seconds > maximumSeconds ||
            kilometers > decimal.MaxValue / 1000m)
        {
            throw new TravelGraphUnavailableException();
        }

        var ticks = decimal.Truncate(
            seconds * TimeSpan.TicksPerSecond);

        return TravelCost.Create(
            TimeSpan.FromTicks((long)ticks),
            kilometers * 1000m);
    }
}