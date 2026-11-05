using ErrandFlow.Application.Errands.Dtos;
using ErrandFlow.Application.Plans;

namespace ErrandFlow.Application.Errands.UseCases;

public sealed class GetErrand
{
    private readonly IPlanRepository _planRepository;

    public GetErrand(IPlanRepository planRepository)
    {
        _planRepository = planRepository;
    }

    public async Task<ErrandDto?> ExecuteAsync(
        Guid ownerId,
        Guid planId,
        Guid errandId,
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

        var plan = await _planRepository.GetWithErrandsAsync(
            ownerId,
            planId,
            cancellationToken);

        if (plan is null)
        {
            return null;
        }

        var errand = plan.Errands.SingleOrDefault(
            item => item.Id == errandId);

        return errand is null ? null : ErrandMapper.ToDto(errand);
    }
}