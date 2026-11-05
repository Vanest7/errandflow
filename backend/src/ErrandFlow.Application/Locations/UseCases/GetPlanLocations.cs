using ErrandFlow.Application.Locations.Dtos;
using ErrandFlow.Application.Plans;

namespace ErrandFlow.Application.Locations.UseCases;

public sealed class GetPlanLocations
{
    private readonly ILocationRepository _locationRepository;
    private readonly IPlanRepository _planRepository;

    public GetPlanLocations(
        ILocationRepository locationRepository,
        IPlanRepository planRepository)
    {
        _locationRepository = locationRepository;
        _planRepository = planRepository;
    }

    public async Task<IReadOnlyList<LocationDto>?> ExecuteAsync(
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

        var plan = await _planRepository.GetByIdAsync(
            ownerId,
            planId,
            cancellationToken);

        if (plan is null)
        {
            return null;
        }

        var locations = await _locationRepository.GetByPlanAsync(
            ownerId,
            planId,
            cancellationToken);

        return locations
            .Select(LocationMapper.ToDto)
            .ToList();
    }
}
