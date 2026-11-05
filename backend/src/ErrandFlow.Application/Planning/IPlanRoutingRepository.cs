using ErrandFlow.Application.Planning.Dtos;

namespace ErrandFlow.Application.Planning;

public interface IPlanRoutingRepository
{
    Task<PlanRoutingSnapshot?> GetSnapshotAsync(
        Guid ownerId,
        Guid planId,
        CancellationToken cancellationToken = default);
}