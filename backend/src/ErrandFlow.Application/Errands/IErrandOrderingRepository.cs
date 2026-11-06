namespace ErrandFlow.Application.Errands;

public interface IErrandOrderingRepository
{
    Task<bool> ReorderErrandsAsync(
        Guid ownerId,
        Guid planId,
        ulong expectedRevision,
        IReadOnlyList<Guid> orderedErrandIds,
        CancellationToken cancellationToken = default);
}
