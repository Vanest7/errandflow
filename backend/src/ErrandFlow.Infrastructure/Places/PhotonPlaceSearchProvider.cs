using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using ErrandFlow.Application.Places;
using ErrandFlow.Application.Places.Dtos;
using ErrandFlow.Application.Places.Exceptions;

namespace ErrandFlow.Infrastructure.Places;

public sealed class PhotonPlaceSearchProvider : IPlaceSearchProvider
{
    private readonly HttpClient _httpClient;
    private readonly PhotonRequestGate _gate;

    public PhotonPlaceSearchProvider(HttpClient httpClient, PhotonRequestGate gate)
    {
        _httpClient = httpClient;
        _gate = gate;
    }

    public async Task<IReadOnlyList<PlaceSearchResultDto>> SearchAsync(
        PlaceSearchInput input,
        CancellationToken cancellationToken = default)
    {
        await _gate.EnterAsync(cancellationToken);
        try
        {
            var parameters = new List<KeyValuePair<string, string>>
            {
                new("limit", input.Limit.ToString(CultureInfo.InvariantCulture))
            };
            var nearby = input.Category is not null;
            if (nearby)
            {
                parameters.Add(new("osm_tag", PhotonCategoryTags.GetTag(input.Category!)));
                parameters.Add(new("radius", Format(input.RadiusKilometers)));
            }
            else
            {
                parameters.Add(new("q", input.Query!));
            }

            if (input.Latitude.HasValue && input.Longitude.HasValue)
            {
                parameters.Add(new("lat", Format(input.Latitude.Value)));
                parameters.Add(new("lon", Format(input.Longitude.Value)));
            }

            if (input.BoundingBox is not null)
            {
                parameters.Add(new("bbox", input.BoundingBox));
            }

            var query = string.Join("&", parameters.Select(parameter =>
                $"{parameter.Key}={Uri.EscapeDataString(parameter.Value)}"));
            var path = nearby ? "reverse" : "api/";
            using var response = await _httpClient.GetAsync($"{path}?{query}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new PlaceSearchUnavailableException();
            }

            var payload = await response.Content.ReadFromJsonAsync<PhotonResponse>(
                cancellationToken: cancellationToken);
            if (payload?.Features is null)
            {
                throw new PlaceSearchUnavailableException();
            }

            var results = new List<PlaceSearchResultDto>();
            foreach (var feature in payload.Features)
            {
                if (feature is null) continue;
                var coordinates = feature.Geometry?.Coordinates;
                var properties = feature.Properties;
                if (feature.Geometry?.Type != "Point" || coordinates is not { Length: >= 2 } ||
                    properties is null || coordinates[0] is < -180 or > 180 ||
                    coordinates[1] is < -90 or > 90)
                {
                    continue;
                }

                var street = Join(" ", properties.Street, properties.Housenumber);
                var address = Join(", ", street, properties.Postcode,
                    properties.City ?? properties.District, properties.Country);
                var label = string.IsNullOrWhiteSpace(properties.Name) ? address : properties.Name.Trim();
                if (string.IsNullOrWhiteSpace(label))
                {
                    continue;
                }

                var externalId = properties.OsmId.HasValue && properties.OsmType is not null
                    ? $"osm:{properties.OsmType}:{properties.OsmId.Value.ToString(CultureInfo.InvariantCulture)}"
                    : null;
                results.Add(new PlaceSearchResultDto(externalId,
                    label.Length > 150 ? label[..150] : label,
                    string.IsNullOrEmpty(address) ? null : address.Length > 500 ? address[..500] : address,
                    decimal.Round(coordinates[1], 6), decimal.Round(coordinates[0], 6)));
                if (results.Count == input.Limit) break;
            }
            return results;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new PlaceSearchUnavailableException();
        }
        catch (HttpRequestException)
        {
            throw new PlaceSearchUnavailableException();
        }
        catch (JsonException)
        {
            throw new PlaceSearchUnavailableException();
        }
        finally
        {
            _gate.Exit();
        }
    }

    private static string Format(decimal value) => value.ToString(CultureInfo.InvariantCulture);
    private static string Join(string separator, params string?[] values) =>
        string.Join(separator, values.Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim()));
}
