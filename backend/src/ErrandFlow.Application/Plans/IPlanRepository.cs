using System;
using ErrandFlow.Domain.Plans;

namespace ErrandFlow.Application.Plans;

public interface IPlanRepository
{
    Task<IReadOnlyList<Plan>> GetByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<Plan?> GetByIdAsync(
        Guid ownerId,
        Guid planId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Plan plan,
        CancellationToken cancellationToken = default);

    void Remove(Plan plan);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<Plan?> GetWithErrandsAsync(
    Guid ownerId,
    Guid planId,
    CancellationToken cancellationToken = default);
}
