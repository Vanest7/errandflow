using ErrandFlow.Application.Plans.Dtos;
using ErrandFlow.Domain.Plans;

namespace ErrandFlow.Application.Plans.UseCases;

public sealed class CreatePlan
{
    private readonly IPlanRepository _planRepository;

    public CreatePlan(IPlanRepository planRepository)
    {
        _planRepository = planRepository;
    }

    public async Task<PlanDto> ExecuteAsync(
        Guid ownerId,
        PlanInput input,
        CancellationToken cancellationToken = default)
    {
        ValidateOwner(ownerId);
        ArgumentNullException.ThrowIfNull(input);

        var plan = new Plan(
            ownerId,
            input.Name,
            input.PlanDate,
            input.DepartureTime,
            input.TimeZone);

        await _planRepository.AddAsync(plan, cancellationToken);
        await _planRepository.SaveChangesAsync(cancellationToken);

        return PlanMapper.ToDto(plan);
    }

    private static void ValidateOwner(Guid ownerId)
    {
        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Owner ID cannot be empty.",
                nameof(ownerId));
        }
    }
}
