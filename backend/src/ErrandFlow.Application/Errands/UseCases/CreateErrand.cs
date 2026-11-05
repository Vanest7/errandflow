using ErrandFlow.Application.Errands.Dtos;
using ErrandFlow.Application.Locations;
using ErrandFlow.Application.Plans;
using ErrandFlow.Application.Plans.Exceptions;
using ErrandFlow.Domain.Locations;

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

        if (input.ExpectedRevision == 0)
        {
            throw new ArgumentException(
                "Expected revision must be greater than zero.",
                "expectedRevision");
        }

        if (input.LocationId.HasValue == (input.NewLocation is not null))
        {
            throw new ArgumentException(
                "Provide either a location ID or a new location, but not both.",
                nameof(input));
        }

        if (input.LocationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Location ID cannot be empty.",
                "locationId");
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

        Location location;

        if (input.NewLocation is { } newLocation)
        {
            location = new Location(
                plan.Id,
                newLocation.Label,
                newLocation.Address,
                newLocation.Latitude,
                newLocation.Longitude);
        }
        else
        {
            var existingLocation = await _locationRepository.GetByIdAsync(
                ownerId,
                planId,
                input.LocationId!.Value,
                cancellationToken);

            if (existingLocation is null)
            {
                throw new ArgumentException(
                    "The location is not available in this plan.",
                    "locationId");
            }

            location = existingLocation;
        }

        var errand = plan.AddErrand(
            location,
            input.Title,
            input.DurationMinutes);

        if (input.NewLocation is not null)
        {
            await _locationRepository.AddAsync(
                location,
                cancellationToken);
        }

        await _planRepository.SaveChangesAsync(cancellationToken);

        return new ErrandMutationResult(
            ErrandMapper.ToDto(errand),
            plan.Revision);
    }
}