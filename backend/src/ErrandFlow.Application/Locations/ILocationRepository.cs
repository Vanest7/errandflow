using ErrandFlow.Domain.Locations;

namespace ErrandFlow.Application.Locations;

public interface ILocationRepository
{
    Task<Location?> GetByIdAsync(
        Guid ownerId,
        Guid planId,
        Guid locationId,
        CancellationToken cancellationToken = default);

        Task AddAsync(
    Location location,
    CancellationToken cancellationToken = default);
}