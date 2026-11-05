using ErrandFlow.Application.Places.Dtos;

namespace ErrandFlow.Application.Places;

public interface IPlaceSearchProvider
{
    Task<IReadOnlyList<PlaceSearchResultDto>> SearchAsync(
        PlaceSearchInput input,
        CancellationToken cancellationToken = default);
}
