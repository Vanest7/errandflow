using System.Text.Json.Serialization;

namespace ErrandFlow.Infrastructure.Routing.Valhalla;

internal sealed class ValhallaMatrixCell
{
    [JsonRequired]
    [JsonPropertyName("from_index")]
    public int FromIndex { get; init; }

    [JsonRequired]
    [JsonPropertyName("to_index")]
    public int ToIndex { get; init; }

    [JsonRequired]
    [JsonPropertyName("time")]
    public decimal? TimeSeconds { get; init; }

    [JsonRequired]
    [JsonPropertyName("distance")]
    public decimal? Distance { get; init; }
}