using ErrandFlow.Application.Locations.Dtos;
using ErrandFlow.Domain.Locations;

namespace ErrandFlow.Application.Locations;

internal static class LocationMapper
{
    public static LocationDto ToDto(Location location)
    {
        return new LocationDto(
            location.Id,
            location.PlanId,
            location.Label,
            location.Address,
            location.Latitude,
            location.Longitude);
    }
}