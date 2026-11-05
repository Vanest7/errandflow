using System.Text.Json.Serialization;

namespace ErrandFlow.Infrastructure.Places;

internal sealed class PhotonResponse
{
    public List<PhotonFeature?>? Features { get; init; }
}

internal sealed class PhotonFeature
{
    public PhotonGeometry? Geometry { get; init; }
    public PhotonProperties? Properties { get; init; }
}

internal sealed class PhotonGeometry
{
    public string? Type { get; init; }
    public decimal[]? Coordinates { get; init; }
}

internal sealed class PhotonProperties
{
    public string? Name { get; init; }
    public string? Street { get; init; }
    public string? Housenumber { get; init; }
    public string? Postcode { get; init; }
    public string? City { get; init; }
    public string? District { get; init; }
    public string? Country { get; init; }

    [JsonPropertyName("osm_type")]
    public string? OsmType { get; init; }

    [JsonPropertyName("osm_id")]
    public long? OsmId { get; init; }
}
