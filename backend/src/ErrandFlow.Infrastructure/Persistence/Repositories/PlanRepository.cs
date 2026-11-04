using ErrandFlow.Application.Plans;
using ErrandFlow.Application.Plans.Exceptions;
using ErrandFlow.Domain.Plans;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using ErrandFlow.Domain.Errands;

using MySqlConnector;

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

   public async Task SaveChangesAsync(
    CancellationToken cancellationToken = default)
{
    try
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateConcurrencyException exception)
    {
        var conflict = CreatePlanConflict(exception);

        if (conflict is null)
        {
            throw;
        }

        throw conflict;
    }
    catch (DbUpdateException exception)
        when (IsErrandPositionConflict(exception))
    {
        var conflict = CreatePlanConflict(exception);

        if (conflict is null)
        {
            throw;
        }

        throw conflict;
    }
}

    public Task<Plan?> GetWithErrandsAsync(
    Guid ownerId,
    Guid planId,
    CancellationToken cancellationToken = default)
{
    return _dbContext.Plans
        .AsTracking()
        .AsSingleQuery()
        .Include(plan => plan.Errands)
        .SingleOrDefaultAsync(
            plan => plan.OwnerId == ownerId && plan.Id == planId,
            cancellationToken);
}
private PlanConcurrencyException? CreatePlanConflict(
    DbUpdateException exception)
{
    if (exception.Entries.Count == 0 ||
        exception.Entries.Any(entry =>
            entry.Entity is not Plan &&
            entry.Entity is not Errand))
    {
        return null;
    }

    var planIds = exception.Entries
        .Select(entry => entry.Entity switch
        {
            Plan plan => plan.Id,
            Errand errand => errand.PlanId,
            _ => Guid.Empty
        })
        .Distinct()
        .ToArray();

    if (planIds.Length != 1)
    {
        return null;
    }

    var planId = planIds[0];

    var planEntry = _dbContext.ChangeTracker
        .Entries<Plan>()
        .SingleOrDefault(entry => entry.Entity.Id == planId);

    if (planEntry is null ||
        planEntry.State == EntityState.Added)
    {
        return null;
    }

    var expectedRevision = planEntry
        .Property(plan => plan.Revision)
        .OriginalValue;

    return new PlanConcurrencyException(
        planId,
        expectedRevision,
        exception);
}
private static bool IsErrandPositionConflict(
    DbUpdateException exception)
{
    if (exception.InnerException is not MySqlException
        {
            Number: 1062
        } databaseException)
    {
        return false;
    }

    if (!exception.Entries.Any(entry => entry.Entity is Errand))
    {
        return false;
    }

    return Regex.IsMatch(
        databaseException.Message,
        @"for key ['`](?:[^'`]+\.)?uq_errands_position['`]\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));
}
}
