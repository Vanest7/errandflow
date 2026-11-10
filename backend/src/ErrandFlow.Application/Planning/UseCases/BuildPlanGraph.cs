using ErrandFlow.Application.Planning.Dtos;
using ErrandFlow.Domain.Planning;

namespace ErrandFlow.Application.Planning.UseCases;

public sealed class BuildPlanGraph
{
    private readonly IPlanRoutingRepository _repository;
    private readonly ITravelGraphProvider _provider;

    public BuildPlanGraph(
        IPlanRoutingRepository repository,
        ITravelGraphProvider provider)
    {
        _repository = repository;
        _provider = provider;
    }

    public async Task<PlanGraphResult?> ExecuteAsync(
        Guid ownerId,
        Guid planId,
        TravelMode travelMode,
        CancellationToken cancellationToken = default)
    {
        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Owner ID cannot be empty.",
                nameof(ownerId));
        }

        if (planId == Guid.Empty)
        {
            throw new ArgumentException(
                "Plan ID cannot be empty.",
                nameof(planId));
        }

        if (travelMode is not (TravelMode.Walking or TravelMode.Driving))
        {
            throw new ArgumentException(
                "Only walking and driving routes are currently supported.",
                nameof(travelMode));
        }

        var snapshot = await _repository.GetSnapshotAsync(
            ownerId,
            planId,
            cancellationToken);

        if (snapshot is null)
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var locationIds = snapshot.Locations
            .Select(location => location.LocationId)
            .ToArray();

        LocationGraph graph;

        if (locationIds.Length <= 1)
        {
            graph = new LocationGraph(
                locationIds,
                new Dictionary<(Guid From, Guid To), TravelCost>());
        }
        else
        {
            graph = await _provider.BuildAsync(
                snapshot.Locations,
                travelMode,
                cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (graph is null ||
            !locationIds.ToHashSet().SetEquals(graph.LocationIds))
        {
            throw new InvalidOperationException(
                "The travel provider returned an unexpected set of locations.");
        }

        return new PlanGraphResult(
            snapshot.PlanId,
            snapshot.Revision,
            travelMode,
            snapshot.OriginLocationId,
            snapshot.DestinationLocationId,
            graph);
    }
}