using ErrandFlow.Application.Locations.Dtos;
using ErrandFlow.Application.Plans;
using ErrandFlow.Application.Plans.Exceptions;

namespace ErrandFlow.Application.Locations.UseCases;

public sealed class UpdateLocation(
    IPlanRepository planRepository,
    ILocationRepository locationRepository)
{
    public async Task<LocationMutationResult?> ExecuteAsync(
        Guid ownerId,
        Guid planId,
        Guid locationId,
        UpdateLocationInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ValidateIds(ownerId, planId, locationId);
        ValidateRevision(input.ExpectedRevision);

        var plan = await planRepository.GetByIdAsync(ownerId, planId, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        if (plan.Revision != input.ExpectedRevision)
        {
            throw new PlanConcurrencyException(planId, input.ExpectedRevision);
        }

        var location = await locationRepository.GetByIdForUpdateAsync(
            ownerId, planId, locationId, cancellationToken);
        if (location is null)
        {
            return null;
        }

        location.UpdateDetails(
            input.Label,
            input.Address,
            input.Latitude,
            input.Longitude);
        plan.UpdateRevision();
        await planRepository.SaveChangesAsync(cancellationToken);

        return new LocationMutationResult(LocationMapper.ToDto(location), plan.Revision);
    }

    private static void ValidateIds(Guid ownerId, Guid planId, Guid locationId)
    {
        if (ownerId == Guid.Empty) throw new ArgumentException("Owner ID cannot be empty.", nameof(ownerId));
        if (planId == Guid.Empty) throw new ArgumentException("Plan ID cannot be empty.", nameof(planId));
        if (locationId == Guid.Empty) throw new ArgumentException("Location ID cannot be empty.", nameof(locationId));
    }

    private static void ValidateRevision(ulong revision)
    {
        if (revision == 0) throw new ArgumentException("Expected revision must be greater than zero.", "expectedRevision");
    }
}
