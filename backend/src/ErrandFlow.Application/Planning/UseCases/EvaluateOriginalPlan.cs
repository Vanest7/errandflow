using ErrandFlow.Application.Planning.Dtos;
using ErrandFlow.Application.Plans;
using ErrandFlow.Application.Plans.Exceptions;
using ErrandFlow.Domain.Planning;
using ErrandFlow.Domain.Plans;

namespace ErrandFlow.Application.Planning.UseCases;

public sealed class EvaluateOriginalPlan
{
    private readonly IPlanRepository _planRepository;
    private readonly BuildPlanGraph _buildPlanGraph;

    public EvaluateOriginalPlan(
        IPlanRepository planRepository,
        BuildPlanGraph buildPlanGraph)
    {
        _planRepository = planRepository;
        _buildPlanGraph = buildPlanGraph;
    }

    public async Task<PlanScheduleResult?> ExecuteAsync(
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
                "An origin location is required to evaluate the plan.",
                "originLocationId");
        }

        var departureAtUtc = ResolveDepartureAtUtc(plan);
        var revision = plan.Revision;

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

        var evaluation = OriginalOrderEvaluator.Evaluate(
            originId,
            plan.DestinationLocationId,
            departureAtUtc,
            plan.Errands,
            graphResult.Graph);

        return new PlanScheduleResult(
            plan.Id,
            revision,
            travelMode,
            departureAtUtc,
            evaluation);
    }

    private static DateTime ResolveDepartureAtUtc(Plan plan)
    {
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