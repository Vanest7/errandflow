using ErrandFlow.Application.Plans;
using ErrandFlow.Application.Plans.Exceptions;

namespace ErrandFlow.Application.Plans.UseCases;

public sealed class DeletePlan
{
    private readonly IPlanRepository _planRepository;

    public DeletePlan(IPlanRepository planRepository)
    {
        _planRepository = planRepository;
    }

    public async Task<bool> ExecuteAsync(
        Guid ownerId,
        Guid planId,
        ulong? expectedRevision,
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

        if (!expectedRevision.HasValue || expectedRevision.Value == 0)
        {
            throw new ArgumentException(
                "A positive expected revision is required.",
                nameof(expectedRevision));
        }

        return await _planRepository.DeleteAsync(
            ownerId,
            planId,
            expectedRevision.Value,
            cancellationToken);
    }
}