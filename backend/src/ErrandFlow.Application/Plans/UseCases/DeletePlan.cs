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

        var plan = await _planRepository.GetByIdAsync(
            ownerId,
            planId,
            cancellationToken);

        if (plan is null)
        {
            return false;
        }

        _planRepository.Remove(plan);
        await _planRepository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
