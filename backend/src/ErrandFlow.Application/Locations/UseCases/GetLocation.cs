using ErrandFlow.Application.Locations.Dtos;

namespace ErrandFlow.Application.Locations.UseCases;

public sealed class GetLocation
{
    private readonly ILocationRepository _locationRepository;

    public GetLocation(ILocationRepository locationRepository)
    {
        _locationRepository = locationRepository;
    }

    public async Task<LocationDto?> ExecuteAsync(
        Guid ownerId,
        Guid planId,
        Guid locationId,
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

        if (locationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Location ID cannot be empty.",
                nameof(locationId));
        }

        var location = await _locationRepository.GetByIdAsync(
            ownerId,
            planId,
            locationId,
            cancellationToken);

        return location is null
            ? null
            : LocationMapper.ToDto(location);
    }
}