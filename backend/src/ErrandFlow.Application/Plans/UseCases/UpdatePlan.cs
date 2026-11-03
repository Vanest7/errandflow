using ErrandFlow.Application.Plans.Dtos;
using ErrandFlow.Application.Plans.Exceptions;

namespace ErrandFlow.Application.Plans.UseCases;

public sealed class UpdatePlan
{
    private readonly IPlanRepository _planRepository;

    public UpdatePlan(IPlanRepository planRepository)
    {
        _planRepository = planRepository;
    }

    public async Task<PlanDto?> ExecuteAsync(
        Guid ownerId,
        Guid planId,
        UpdatePlanInput input,
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

        ArgumentNullException.ThrowIfNull(input);

        var plan = await _planRepository.GetByIdAsync(
            ownerId,
            planId,
            cancellationToken);

        if (plan is null)
        {
            return null;
        }

        if (plan.Revision != input.ExpectedRevision)
        {
            throw new PlanConcurrencyException(
                planId,
                input.ExpectedRevision);
        }

        plan.UpdateDetails(
            input.Name,
            input.PlanDate,
            input.DepartureTime,
            input.TimeZone);

        await _planRepository.SaveChangesAsync(cancellationToken);

        return PlanMapper.ToDto(plan);
    }
}
