using ErrandFlow.Application.Plans.Dtos;

namespace ErrandFlow.Application.Plans.UseCases;

public sealed class GetPlan
{
    private readonly IPlanRepository _planRepository;

    public GetPlan(IPlanRepository planRepository)
    {
        _planRepository = planRepository;
    }

    public async Task<PlanDto?> ExecuteAsync(
        Guid ownerId,
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        ValidateIds(ownerId, planId);

        var plan = await _planRepository.GetByIdAsync(
            ownerId,
            planId,
            cancellationToken);

        return plan is null ? null : PlanMapper.ToDto(plan);
    }

    private static void ValidateIds(Guid ownerId, Guid planId)
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
    }
}
