using ErrandFlow.Application.Plans;
using ErrandFlow.Application.Plans.Exceptions;

namespace ErrandFlow.Application.Errands.UseCases;

public sealed class DeleteErrand
{
    private readonly IPlanRepository _planRepository;

    public DeleteErrand(IPlanRepository planRepository)
    {
        _planRepository = planRepository;
    }

    public async Task<bool> ExecuteAsync(
        Guid ownerId,
        Guid planId,
        Guid errandId,
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

        if (errandId == Guid.Empty)
        {
            throw new ArgumentException(
                "Errand ID cannot be empty.",
                nameof(errandId));
        }

        if (!expectedRevision.HasValue || expectedRevision.Value == 0)
        {
            throw new ArgumentException(
                "A positive expected revision is required.",
                nameof(expectedRevision));
        }

        var plan = await _planRepository.GetWithErrandsAsync(
            ownerId,
            planId,
            cancellationToken);

        if (plan is null ||
            !plan.Errands.Any(errand => errand.Id == errandId))
        {
            return false;
        }

        if (plan.Revision != expectedRevision.Value)
        {
            throw new PlanConcurrencyException(
                planId,
                expectedRevision.Value);
        }

        if (!plan.RemoveErrand(errandId))
        {
            return false;
        }

        await _planRepository.SaveChangesAsync(cancellationToken);

        return true;
    }
}