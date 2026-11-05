using ErrandFlow.Application.Errands.Dtos;
using ErrandFlow.Application.Plans;

namespace ErrandFlow.Application.Errands.UseCases;

public sealed class GetPlanErrands
{
    private readonly IPlanRepository _planRepository;

    public GetPlanErrands(IPlanRepository planRepository)
    {
        _planRepository = planRepository;
    }

    public async Task<IReadOnlyList<ErrandDto>?> ExecuteAsync(
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

        var plan = await _planRepository.GetWithErrandsAsync(
            ownerId,
            planId,
            cancellationToken);

        if (plan is null)
        {
            return null;
        }

        return plan.Errands
            .OrderBy(errand => errand.OriginalPosition)
            .Select(ErrandMapper.ToDto)
            .ToList();
    }
}