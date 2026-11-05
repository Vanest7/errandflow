using ErrandFlow.Application.Locations.Dtos;
using ErrandFlow.Application.Plans;
using ErrandFlow.Application.Plans.Exceptions;

namespace ErrandFlow.Application.Locations.UseCases;

public sealed class CreateLocation
{
    private readonly IPlanRepository _planRepository;
    private readonly ILocationRepository _locationRepository;

    public CreateLocation(
        IPlanRepository planRepository,
        ILocationRepository locationRepository)
    {
        _planRepository = planRepository;
        _locationRepository = locationRepository;
    }

    public async Task<LocationMutationResult?> ExecuteAsync(
        Guid ownerId,
        Guid planId,
        CreateLocationInput input,
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

        if (input.ExpectedRevision == 0)
        {
            throw new ArgumentException(
                "Expected revision must be greater than zero.",
                "expectedRevision");
        }

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

        var location = plan.CreateLocation(
            input.Label,
            input.Address,
            input.Latitude,
            input.Longitude);

        await _locationRepository.AddAsync(
            location,
            cancellationToken);

        await _planRepository.SaveChangesAsync(cancellationToken);

        return new LocationMutationResult(
            LocationMapper.ToDto(location),
            plan.Revision);
    }
}