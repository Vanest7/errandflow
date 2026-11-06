using ErrandFlow.Application.Locations.Exceptions;
using ErrandFlow.Application.Plans;
using ErrandFlow.Application.Plans.Exceptions;

namespace ErrandFlow.Application.Locations.UseCases;

public sealed class DeleteLocation(
    IPlanRepository planRepository,
    ILocationRepository locationRepository)
{
    public async Task<bool> ExecuteAsync(
        Guid ownerId,
        Guid planId,
        Guid locationId,
        ulong expectedRevision,
        CancellationToken cancellationToken = default)
    {
        if (ownerId == Guid.Empty) throw new ArgumentException("Owner ID cannot be empty.", nameof(ownerId));
        if (planId == Guid.Empty) throw new ArgumentException("Plan ID cannot be empty.", nameof(planId));
        if (locationId == Guid.Empty) throw new ArgumentException("Location ID cannot be empty.", nameof(locationId));
        if (expectedRevision == 0) throw new ArgumentException("Expected revision must be greater than zero.", nameof(expectedRevision));

        var plan = await planRepository.GetWithErrandsAsync(ownerId, planId, cancellationToken);
        if (plan is null) return false;
        if (plan.Revision != expectedRevision) throw new PlanConcurrencyException(planId, expectedRevision);

        var location = await locationRepository.GetByIdForUpdateAsync(
            ownerId, planId, locationId, cancellationToken);
        if (location is null) return false;
        if (!plan.RemoveLocation(locationId))
        {
            throw new LocationInUseException(locationId);
        }

        locationRepository.Remove(location);
        await planRepository.SaveChangesAsync(cancellationToken);
        return true;
    }
}
