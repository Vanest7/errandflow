using ErrandFlow.Application.Plans.Dtos;

namespace ErrandFlow.Application.Plans.UseCases;

public sealed class GetMyPlans
{
    private readonly IPlanRepository _planRepository;

    public GetMyPlans(IPlanRepository planRepository)
    {
        _planRepository = planRepository;
    }

    public async Task<IReadOnlyList<PlanSummaryDto>> ExecuteAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Owner ID cannot be empty.",
                nameof(ownerId));
        }

        var plans = await _planRepository.GetByOwnerAsync(
            ownerId,
            cancellationToken);

        return plans
            .Select(PlanMapper.ToSummaryDto)
            .ToList();
    }
}