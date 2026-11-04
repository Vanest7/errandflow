using ErrandFlow.Application.Locations;
using ErrandFlow.Domain.Locations;
using Microsoft.EntityFrameworkCore;

namespace ErrandFlow.Infrastructure.Persistence.Repositories;

public sealed class LocationRepository : ILocationRepository
{
    private readonly ErrandFlowDbContext _dbContext;

    public LocationRepository(ErrandFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Location?> GetByIdAsync(
        Guid ownerId,
        Guid planId,
        Guid locationId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Locations
            .AsNoTracking()
            .Where(location =>
                location.Id == locationId &&
                location.PlanId == planId)
            .Where(location =>
                _dbContext.Plans.Any(plan =>
                    plan.Id == location.PlanId &&
                    plan.OwnerId == ownerId))
            .SingleOrDefaultAsync(cancellationToken);
    }
}