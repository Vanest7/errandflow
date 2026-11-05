using ErrandFlow.Application.Locations;
using ErrandFlow.Application.Plans.Dtos;
using ErrandFlow.Application.Plans.Exceptions;

namespace ErrandFlow.Application.Plans.UseCases;

public sealed class SetPlanLocations
{
    private readonly IPlanRepository _planRepository;
    private readonly ILocationRepository _locationRepository;

    public SetPlanLocations(
        IPlanRepository planRepository,
        ILocationRepository locationRepository)
    {
        _planRepository = planRepository;
        _locationRepository = locationRepository;
    }

    public async Task<PlanDto?> ExecuteAsync(
        Guid ownerId,
        Guid planId,
        SetPlanLocationsInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

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

        if (input.ExpectedRevision == 0)
        {
            throw new ArgumentException(
                "Expected revision must be greater than zero.",
                nameof(input.ExpectedRevision));
        }

        ValidateLocationId(input.OriginLocationId, nameof(input.OriginLocationId));
        ValidateLocationId(
            input.DestinationLocationId,
            nameof(input.DestinationLocationId));

        var plan = await _planRepository.GetByIdAsync(
            ownerId,
            planId,
            cancellationToken);

        if (plan is null)
        {
            return null;
        }

        if (plan.Revision != input.ExpectedRevision)
        {
            throw new PlanConcurrencyException(
                planId,
                input.ExpectedRevision);
        }

        await EnsureLocationBelongsToPlanAsync(
            ownerId,
            planId,
            input.OriginLocationId,
            nameof(input.OriginLocationId),
            cancellationToken);
        await EnsureLocationBelongsToPlanAsync(
            ownerId,
            planId,
            input.DestinationLocationId,
            nameof(input.DestinationLocationId),
            cancellationToken);

        plan.SetLocations(
            input.OriginLocationId,
            input.DestinationLocationId);

        await _planRepository.SaveChangesAsync(cancellationToken);

        return PlanMapper.ToDto(plan);
    }

    private async Task EnsureLocationBelongsToPlanAsync(
        Guid ownerId,
        Guid planId,
        Guid? locationId,
        string parameterName,
        CancellationToken cancellationToken)
    {
        if (!locationId.HasValue)
        {
            return;
        }

        var location = await _locationRepository.GetByIdAsync(
            ownerId,
            planId,
            locationId.Value,
            cancellationToken);

        if (location is null)
        {
            throw new ArgumentException(
                "The location is not available in this plan.",
                parameterName);
        }
    }

    private static void ValidateLocationId(
        Guid? locationId,
        string parameterName)
    {
        if (locationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Location ID cannot be empty.",
                parameterName);
        }
    }
}
