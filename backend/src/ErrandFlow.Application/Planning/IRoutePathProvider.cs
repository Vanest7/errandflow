using ErrandFlow.Application.Planning.Dtos;
using ErrandFlow.Domain.Planning;

namespace ErrandFlow.Application.Planning;

public interface IRoutePathProvider
{
    /// <summary>
    /// Builds a route through the supplied locations in their given order.
    /// Consecutive entries with the same location ID must be collapsed
    /// before calling this method. Nonconsecutive repeated visits are allowed.
    /// Zero or one location produces a path with no legs.
    /// </summary>
    Task<RoutePathDto> BuildAsync(
        IReadOnlyList<RoutingLocation> orderedLocations,
        TravelMode travelMode,
        CancellationToken cancellationToken = default);
}