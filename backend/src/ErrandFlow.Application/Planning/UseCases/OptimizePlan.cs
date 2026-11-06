using ErrandFlow.Application.Planning.Dtos;
using ErrandFlow.Application.Plans;
using ErrandFlow.Application.Plans.Exceptions;
using ErrandFlow.Domain.Planning;

namespace ErrandFlow.Application.Planning.UseCases;

public sealed class OptimizePlan
{
    private readonly IPlanRepository _planRepository;
    private readonly BuildPlanGraph _buildPlanGraph;

    public OptimizePlan(
        IPlanRepository planRepository,
        BuildPlanGraph buildPlanGraph)
    {
        _planRepository = planRepository;
        _buildPlanGraph = buildPlanGraph;
    }

    public async Task<PlanOptimizationResult?> ExecuteAsync(
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

        if (travelMode != TravelMode.Walking)
        {
            throw new ArgumentException(
                "Only walking routes are currently supported.",
                nameof(travelMode));
        }

        var plan = await _planRepository.GetWithErrandsAsync(
            ownerId,
            planId,
            cancellationToken);

        if (plan is null)
        {
            return null;
        }

        if (plan.OriginLocationId is not Guid originId ||
            originId == Guid.Empty)
        {
            throw new ArgumentException(
                "An origin location is required to optimize the plan.",
                "originLocationId");
        }

        if (plan.Errands.Count > ScheduleOptimizer.MaximumErrands)
        {
            throw new ArgumentException(
                $"Optimization supports at most "
                + $"{ScheduleOptimizer.MaximumErrands} errands.",
                "errands");
        }

        var departureAtUtc =
            PlanDepartureTimeResolver.ResolveUtc(plan);

        var revision = plan.Revision;
        var destinationLocationId = plan.DestinationLocationId;
        var errands = plan.Errands.ToArray();

        cancellationToken.ThrowIfCancellationRequested();

        var graphResult = await _buildPlanGraph.ExecuteAsync(
            ownerId,
            planId,
            travelMode,
            cancellationToken);

        if (graphResult is null)
        {
            return null;
        }

        if (graphResult.Revision != revision)
        {
            throw new PlanConcurrencyException(
                planId,
                revision);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var original = OriginalOrderEvaluator.Evaluate(
            originId,
            destinationLocationId,
            departureAtUtc,
            errands,
            graphResult.Graph);

        var optimization = ScheduleOptimizer.Optimize(
            originId,
            destinationLocationId,
            departureAtUtc,
            errands,
            graphResult.Graph,
            cancellationToken: cancellationToken);

        return new PlanOptimizationResult(
            plan.Id,
            revision,
            travelMode,
            departureAtUtc,
            original,
            optimization);
    }
}