using ErrandFlow.Application.Errands.Dtos;
using ErrandFlow.Application.Locations;
using ErrandFlow.Application.Plans;
using ErrandFlow.Application.Plans.Exceptions;

namespace ErrandFlow.Application.Errands.UseCases;

public sealed class CreateErrand
{
    private readonly IPlanRepository _planRepository;
    private readonly ILocationRepository _locationRepository;

    public CreateErrand(
        IPlanRepository planRepository,
        ILocationRepository locationRepository)
    {
        _planRepository = planRepository;
        _locationRepository = locationRepository;
    }

    public async Task<ErrandMutationResult?> ExecuteAsync(
        Guid ownerId,
        Guid planId,
        CreateErrandInput input,
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

        if (input.LocationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Location ID cannot be empty.",
                "locationId");
        }

        if (input.ExpectedRevision == 0)
        {
            throw new ArgumentException(
                "Expected revision must be greater than zero.",
                "expectedRevision");
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

        var location = await _locationRepository.GetByIdAsync(
            ownerId,
            planId,
            input.LocationId,
            cancellationToken);

        if (location is null)
        {
            throw new ArgumentException(
                "The location is not available in this plan.",
                "locationId");
        }

        var errand = plan.AddErrand(
            location,
            input.Title,
            input.DurationMinutes);

        await _planRepository.SaveChangesAsync(cancellationToken);

        return new ErrandMutationResult(
            ErrandMapper.ToDto(errand),
            plan.Revision);
    }
}