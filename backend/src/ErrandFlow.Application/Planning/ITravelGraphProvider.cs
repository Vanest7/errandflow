using ErrandFlow.Application.Planning.Dtos;
using ErrandFlow.Domain.Planning;

namespace ErrandFlow.Application.Planning;

public interface ITravelGraphProvider
{
    Task<LocationGraph> BuildAsync(
        IReadOnlyList<RoutingLocation> locations,
        TravelMode travelMode,
        CancellationToken cancellationToken = default);
}