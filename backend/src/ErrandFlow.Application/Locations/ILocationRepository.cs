using ErrandFlow.Domain.Locations;

namespace ErrandFlow.Application.Locations;

public interface ILocationRepository
{
    Task<IReadOnlyList<Location>> GetByPlanAsync(
        Guid ownerId,
        Guid planId,
        CancellationToken cancellationToken = default);

    Task<Location?> GetByIdAsync(
        Guid ownerId,
        Guid planId,
        Guid locationId,
        CancellationToken cancellationToken = default);

    Task<Location?> GetByIdForUpdateAsync(
        Guid ownerId,
        Guid planId,
        Guid locationId,
        CancellationToken cancellationToken = default);

    void Remove(Location location);

    Task AddAsync(
    Location location,
    CancellationToken cancellationToken = default);
}