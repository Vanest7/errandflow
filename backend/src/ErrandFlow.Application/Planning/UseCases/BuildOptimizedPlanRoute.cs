using ErrandFlow.Application.Planning.Dtos;
using ErrandFlow.Application.Plans;
using ErrandFlow.Application.Plans.Exceptions;

namespace ErrandFlow.Application.Planning.UseCases;

public sealed class BuildOptimizedPlanRoute
{
    private readonly OptimizePlan _optimizePlan;
    private readonly IPlanRepository _planRepository;
    private readonly IPlanRoutingRepository _routingRepository;
    private readonly IRoutePathProvider _routePathProvider;

    public BuildOptimizedPlanRoute(
        OptimizePlan optimizePlan,
        IPlanRepository planRepository,
        IPlanRoutingRepository routingRepository,
        IRoutePathProvider routePathProvider)
    {
        _optimizePlan = optimizePlan;
        _planRepository = planRepository;
        _routingRepository = routingRepository;
        _routePathProvider = routePathProvider;
    }

    public async Task<OptimizedPlanRouteDto?> ExecuteAsync(
        Guid ownerId,
        Guid planId,
        TravelMode travelMode,
        CancellationToken cancellationToken = default)
    {
        var result = await _optimizePlan.ExecuteAsync(
            ownerId,
            planId,
            travelMode,
            cancellationToken);

        if (result is null)
        {
            return null;
        }

        var planning = PlanOptimizationMapper.ToDto(result);
        var best = result.Optimization.BestSchedule;

        if (best is null)
        {
            return new OptimizedPlanRouteDto(
                planning,
                Path: null,
                RouteSchedule: null);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var plan = await _planRepository.GetWithErrandsAsync(
            ownerId,
            planId,
            cancellationToken);

        if (plan is null)
        {
            return null;
        }

        if (plan.Revision != result.Revision)
        {
            throw new PlanConcurrencyException(
                planId,
                result.Revision);
        }

        var snapshot = await _routingRepository.GetSnapshotAsync(
            ownerId,
            planId,
            cancellationToken);

        if (snapshot is null)
        {
            return null;
        }

        if (snapshot.Revision != result.Revision)
        {
            throw new PlanConcurrencyException(
                planId,
                result.Revision);
        }

        if (snapshot.OriginLocationId is not Guid originId ||
            originId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "The evaluated plan must have an origin location.");
        }

        var errandsById = plan.Errands.ToDictionary(
            errand => errand.Id);

        var orderedIds = best.ScheduledErrands
            .Select(errand => errand.ErrandId)
            .ToArray();

        if (orderedIds.Length != errandsById.Count ||
            orderedIds.Distinct().Count() != orderedIds.Length ||
            orderedIds.Any(id => !errandsById.ContainsKey(id)))
        {
            throw new InvalidOperationException(
                "The optimized schedule must contain every plan errand "
                + "exactly once.");
        }

        var orderedErrands = orderedIds
            .Select(id => errandsById[id])
            .ToArray();

        var stops = RouteStopBuilder.Build(
            originId,
            snapshot.DestinationLocationId,
            best,
            snapshot.Locations);

        var path = await _routePathProvider.BuildAsync(
            stops,
            travelMode,
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var routeEvaluation = RouteScheduleEvaluator.Evaluate(
            originId,
            snapshot.DestinationLocationId,
            result.DepartureAtUtc,
            orderedErrands,
            path);

        var routeSchedule = PlanScheduleMapper.ToDto(
            new PlanScheduleResult(
                result.PlanId,
                result.Revision,
                result.TravelMode,
                result.DepartureAtUtc,
                routeEvaluation));

        return new OptimizedPlanRouteDto(
            planning,
            path,
            routeSchedule);
    }
}