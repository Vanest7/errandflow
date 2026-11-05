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

    public async Task<PlanRoutingSnapshot?> GetSnapshotAsync(
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

        var query =
            from plan in _dbContext.Plans.AsNoTracking()
            where plan.Id == planId && plan.OwnerId == ownerId
            join location in _dbContext.Locations.AsNoTracking()
                on plan.Id equals location.PlanId into planLocations
            from location in planLocations.DefaultIfEmpty()
            select new
            {
                PlanId = plan.Id,
                plan.Revision,
                plan.OriginLocationId,
                plan.DestinationLocationId,

                LocationId = (Guid?)location.Id,
                Latitude = (decimal?)location.Latitude,
                Longitude = (decimal?)location.Longitude,

                IsUsed = location != null &&
                    (
                        location.Id == plan.OriginLocationId ||
                        location.Id == plan.DestinationLocationId ||
                        plan.Errands.Any(errand =>
                            errand.LocationId == location.Id)
                    )
            };

        var rows = await query
            .OrderBy(row => row.LocationId)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return null;
        }

        var header = rows[0];

        var locations = rows
            .Where(row => row.IsUsed && row.LocationId.HasValue)
            .Select(row => new RoutingLocation(
                row.LocationId!.Value,
                row.Latitude!.Value,
                row.Longitude!.Value))
            .ToArray();

        return new PlanRoutingSnapshot(
            header.PlanId,
            header.Revision,
            header.OriginLocationId,
            header.DestinationLocationId,
            locations);
    }
}