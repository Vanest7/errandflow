using ErrandFlow.Application.Plans;
using ErrandFlow.Domain.Plans;
using Microsoft.EntityFrameworkCore;

namespace ErrandFlow.Infrastructure.Persistence.Repositories;

public sealed class PlanRepository : IPlanRepository
{
    private readonly ErrandFlowDbContext _dbContext;

    public PlanRepository(ErrandFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Plan>> GetByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Plans
            .AsNoTracking()
            .Where(plan => plan.OwnerId == ownerId)
            .OrderBy(plan => plan.PlanDate)
            .ThenBy(plan => plan.DepartureTime)
            .ToListAsync(cancellationToken);
    }

    public Task<Plan?> GetByIdAsync(
        Guid ownerId,
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Plans
            .SingleOrDefaultAsync(
                plan => plan.OwnerId == ownerId && plan.Id == planId,
                cancellationToken);
    }

    public async Task AddAsync(
        Plan plan,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Plans.AddAsync(plan, cancellationToken);
    }

    public void Remove(Plan plan)
    {
        _dbContext.Plans.Remove(plan);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}