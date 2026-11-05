using System.Globalization;
using ErrandFlow.Application.Places.Dtos;

namespace ErrandFlow.Application.Places.UseCases;

public sealed class SearchPlaces
{
    private readonly IPlaceSearchProvider _provider;

    public SearchPlaces(IPlaceSearchProvider provider)
    {
        _provider = provider;
    }

    public Task<IReadOnlyList<PlaceSearchResultDto>> ExecuteAsync(
        PlaceSearchInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var query = input.Query?.Trim();
        var category = input.Category?.Trim().ToLowerInvariant();
        var hasQuery = !string.IsNullOrEmpty(query);
        var hasCategory = !string.IsNullOrEmpty(category);

        if (hasQuery == hasCategory)
        {
            throw new ArgumentException("Provide either query or category.", "query");
        }

        if (hasQuery && (query!.Length < 3 || query.Length > 200))
        {
            throw new ArgumentException("Query must contain 3 to 200 characters.", "query");
        }

        if (hasCategory)
        {
            category = PlaceCategories.Resolve(category!);
            if (category is null)
            {
                throw new ArgumentException("Unsupported place category.", "category");
            }
        }
        else
        {
            category = PlaceCategories.Resolve(query!);
            if (category is not null)
            {
                query = null;
                hasCategory = true;
            }
        }

        if (input.Latitude.HasValue != input.Longitude.HasValue ||
            (hasCategory && !input.Latitude.HasValue))
        {
            throw new ArgumentException("Provide both latitude and longitude.", "latitude");
        }

        if (input.Latitude is < -90 or > 90)
        {
            throw new ArgumentException("Latitude is outside its valid range.", "latitude");
        }

        if (input.Longitude is < -180 or > 180)
        {
            throw new ArgumentException("Longitude is outside its valid range.", "longitude");
        }

        if (input.Limit is < 1 or > 10)
        {
            throw new ArgumentException("Limit must be between 1 and 10.", "limit");
        }

        if (input.RadiusKilometers is <= 0 or > 10)
        {
            throw new ArgumentException("Radius must be greater than zero and at most 10 km.", "radiusKilometers");
        }

        string? boundingBox = null;
        if (!string.IsNullOrWhiteSpace(input.BoundingBox))
        {
            var parts = input.BoundingBox.Split(',');
            var bounds = new decimal[4];
            if (hasCategory || parts.Length != 4)
            {
                throw new ArgumentException("Bounding box requires text search and four coordinates.", "boundingBox");
            }
            for (var index = 0; index < 4; index++)
            {
                if (!decimal.TryParse(parts[index], NumberStyles.Float,
                    CultureInfo.InvariantCulture, out bounds[index]))
                {
                    throw new ArgumentException("Invalid bounding box coordinate.", "boundingBox");
                }
            }
            if (bounds[0] < -180 || bounds[2] > 180 || bounds[1] < -90 || bounds[3] > 90 ||
                bounds[0] >= bounds[2] || bounds[1] >= bounds[3])
            {
                throw new ArgumentException("Invalid bounding box extent.", "boundingBox");
            }
            boundingBox = string.Join(",", bounds.Select(value => value.ToString(CultureInfo.InvariantCulture)));
        }

        return _provider.SearchAsync(input with { Query = query, Category = category, BoundingBox = boundingBox }, cancellationToken);
    }
}
