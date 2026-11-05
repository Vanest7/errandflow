using ErrandFlow.Application.Planning;
using ErrandFlow.Application.Planning.Dtos;
using Microsoft.EntityFrameworkCore;

namespace ErrandFlow.Infrastructure.Persistence.Repositories;

public sealed class PlanRoutingRepository : IPlanRoutingRepository
{
    private readonly ErrandFlowDbContext _dbContext;

    public PlanRoutingRepository(ErrandFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PlanRoutingSnapshot?> GetSnapshotAsync(
        Guid ownerId,
        Guid planId,
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

        return _dbContext.Plans
            .AsNoTracking()
            .AsSingleQuery()
            .Where(plan =>
                plan.Id == planId &&
                plan.OwnerId == ownerId)
            .Select(plan => new PlanRoutingSnapshot(
                plan.Id,
                plan.Revision,
                plan.OriginLocationId,
                plan.DestinationLocationId,
                _dbContext.Locations
                    .Where(location =>
                        location.PlanId == plan.Id &&
                        (
                            location.Id == plan.OriginLocationId ||
                            location.Id == plan.DestinationLocationId ||
                            plan.Errands.Any(errand =>
                                errand.LocationId == location.Id)
                        ))
                    .OrderBy(location => location.Id)
                    .Select(location => new RoutingLocation(
                        location.Id,
                        location.Latitude,
                        location.Longitude))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);
    }
}