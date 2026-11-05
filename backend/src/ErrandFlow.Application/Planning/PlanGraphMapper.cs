using ErrandFlow.Application.Planning.Dtos;

namespace ErrandFlow.Application.Planning;

public static class PlanGraphMapper
{
    public static PlanGraphDto ToDto(PlanGraphResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var graph = result.Graph;

        var connections = graph.LocationIds
            .SelectMany(fromId => graph.LocationIds
                .Where(toId => toId != fromId)
                .Select(toId =>
                {
                    var cost = graph.GetCost(fromId, toId);

                    return new TravelConnectionDto(
                        fromId,
                        toId,
                        cost.IsReachable,
                        cost.Duration?.TotalSeconds,
                        cost.DistanceMeters);
                }))
            .ToArray();

        return new PlanGraphDto(
            result.PlanId,
            result.Revision,
            result.TravelMode.ToString(),
            result.OriginLocationId,
            result.DestinationLocationId,
            graph.LocationIds.ToArray(),
            connections);
    }
}