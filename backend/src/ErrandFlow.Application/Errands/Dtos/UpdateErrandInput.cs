using ErrandFlow.Application.Locations.Dtos;

namespace ErrandFlow.Application.Errands.Dtos;

public sealed record UpdateErrandInput(
    string Title,
    ushort DurationMinutes,
    ulong ExpectedRevision,
    Guid? LocationId = null,
    NewLocationInput? NewLocation = null);