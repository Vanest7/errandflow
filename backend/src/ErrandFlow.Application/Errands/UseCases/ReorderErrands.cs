using ErrandFlow.Application.Plans;
using ErrandFlow.Application.Plans.Exceptions;
using ErrandFlow.Application.Errands.Dtos;

namespace ErrandFlow.Application.Errands.UseCases;

public sealed class ReorderErrands(
    IPlanRepository planRepository,
    IErrandOrderingRepository orderingRepository)
{
    public async Task<ReorderErrandsResult?> ExecuteAsync(
        Guid ownerId,
        Guid planId,
        ReorderErrandsInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (ownerId == Guid.Empty) throw new ArgumentException("Owner ID cannot be empty.", nameof(ownerId));
        if (planId == Guid.Empty) throw new ArgumentException("Plan ID cannot be empty.", nameof(planId));
        if (input.ExpectedRevision == 0) throw new ArgumentException("Expected revision must be greater than zero.", nameof(input.ExpectedRevision));

        var plan = await planRepository.GetWithErrandsAsync(ownerId, planId, cancellationToken);
        if (plan is null) return null;
        if (plan.Revision != input.ExpectedRevision)
            throw new PlanConcurrencyException(planId, input.ExpectedRevision);

        plan.ValidateErrandOrder(input.ErrandIds);
        var reordered = await orderingRepository.ReorderErrandsAsync(
            ownerId, planId, input.ExpectedRevision, input.ErrandIds, cancellationToken);

        if (!reordered)
        {
            return null;
        }

        var updatedPlan = await planRepository.GetWithErrandsAsync(
            ownerId,
            planId,
            cancellationToken);

        if (updatedPlan is null)
        {
            return null;
        }

        return new ReorderErrandsResult(
            updatedPlan.Errands.OrderBy(errand => errand.OriginalPosition)
                .Select(ErrandMapper.ToDto).ToList(),
            updatedPlan.Revision);
    }
}
