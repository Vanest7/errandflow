using System.Text.Json.Serialization;

namespace ErrandFlow.Infrastructure.Routing.Valhalla;

internal sealed class ValhallaMatrixResponse
{
    [JsonRequired]
    [JsonPropertyName("sources_to_targets")]
    public List<List<ValhallaMatrixCell?>?>? SourcesToTargets
    {
        get;
        init;
    }

    [JsonRequired]
    [JsonPropertyName("units")]
    public string? Units { get; init; }
}