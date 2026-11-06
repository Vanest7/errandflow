namespace ErrandFlow.Application.Locations.Exceptions;

public sealed class LocationInUseException(Guid locationId)
    : InvalidOperationException(
        $"Location {locationId} is still referenced by the plan.")
{
    public Guid LocationId { get; } = locationId;
}
