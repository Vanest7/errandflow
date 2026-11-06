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

    public Task<Location?> GetByIdForUpdateAsync(
        Guid ownerId,
        Guid planId,
        Guid locationId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Locations
            .Where(location => location.Id == locationId && location.PlanId == planId)
            .Where(location => _dbContext.Plans.Any(plan =>
                plan.Id == location.PlanId && plan.OwnerId == ownerId))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public void Remove(Location location)
    {
        _dbContext.Locations.Remove(location);
    }

    public async Task<IReadOnlyList<Location>> GetByPlanAsync(
        Guid ownerId,
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Locations
            .AsNoTracking()
            .Where(location =>
                location.PlanId == planId &&
                _dbContext.Plans.Any(plan =>
                    plan.Id == location.PlanId &&
                    plan.OwnerId == ownerId))
            .OrderBy(location => location.Label)
            .ThenBy(location => location.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
    Location location,
    CancellationToken cancellationToken = default)
{
    await _dbContext.Locations.AddAsync(
        location,
        cancellationToken);
}
}