using ErrandFlow.Application.Errands.Dtos;
using ErrandFlow.Application.Plans;
using ErrandFlow.Application.Plans.Exceptions;

namespace ErrandFlow.Application.Errands.UseCases;

public sealed class SetErrandSchedule
{
    private readonly IPlanRepository _planRepository;

    public SetErrandSchedule(IPlanRepository planRepository)
    {
        _planRepository = planRepository;
    }

    public async Task<ErrandMutationResult?> ExecuteAsync(
        Guid ownerId,
        Guid planId,
        Guid errandId,
        SetErrandScheduleInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

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

        if (input.ExpectedRevision == 0)
        {
            throw new ArgumentException(
                "Expected revision must be greater than zero.",
                nameof(input.ExpectedRevision));
        }

        var plan = await _planRepository.GetWithErrandsAsync(
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

        var errand = plan.SetErrandSchedule(
            errandId,
            input.EarliestStartAt,
            input.LatestFinishAt,
            input.FixedStartAt);

        if (errand is null)
        {
            return null;
        }

        await _planRepository.SaveChangesAsync(cancellationToken);

        return new ErrandMutationResult(
            ErrandMapper.ToDto(errand),
            plan.Revision);
    }
}
