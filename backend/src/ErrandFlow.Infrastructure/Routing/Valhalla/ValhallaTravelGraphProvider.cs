using System.Net.Http.Json;
using System.Text.Json;
using ErrandFlow.Application.Planning;
using ErrandFlow.Application.Planning.Dtos;
using ErrandFlow.Application.Planning.Exceptions;
using ErrandFlow.Domain.Planning;

namespace ErrandFlow.Infrastructure.Routing.Valhalla;

public sealed class ValhallaTravelGraphProvider : ITravelGraphProvider
{
    private readonly HttpClient _httpClient;
    private readonly ValhallaRequestGate _gate;

    public ValhallaTravelGraphProvider(
     HttpClient httpClient,
     ValhallaRequestGate gate)
    {
        _httpClient = httpClient;
        _gate = gate;
    }

    public async Task<LocationGraph> BuildAsync(
        IReadOnlyList<RoutingLocation> locations,
        TravelMode travelMode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(locations);

        cancellationToken.ThrowIfCancellationRequested();

        var costing = travelMode switch
        {
            TravelMode.Walking => "pedestrian",
            _ => throw new ArgumentException(
                "Only walking routes are currently supported.",
                nameof(travelMode))
        };

        var locationSnapshot = locations.ToArray();

        ValidateLocations(locationSnapshot);

        if (locationSnapshot.Length > 10)
        {
            throw new ArgumentException(
                "Routing supports at most 10 distinct locations.",
                nameof(locations));
        }

        if (locationSnapshot.Length <= 1)
        {
            return new LocationGraph(
                locationSnapshot.Select(location => location.LocationId),
                new Dictionary<(Guid From, Guid To), TravelCost>());
        }

        var coordinates = locationSnapshot
            .Select(location => new
            {
                lat = location.Latitude,
                lon = location.Longitude
            })
            .ToArray();

        var request = new
        {
            sources = coordinates,
            targets = coordinates,
            costing,
            units = "kilometers",
            verbose = true
        };

        await _gate.EnterAsync(cancellationToken);

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "sources_to_targets",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new TravelGraphUnavailableException();
            }

            var matrix =
                await response.Content
                    .ReadFromJsonAsync<ValhallaMatrixResponse>(
                        cancellationToken: cancellationToken);

            if (matrix is null)
            {
                throw new TravelGraphUnavailableException();
            }

            cancellationToken.ThrowIfCancellationRequested();

            return ValhallaMatrixMapper.ToGraph(
                matrix,
                locationSnapshot);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new TravelGraphUnavailableException();
        }
        catch (HttpRequestException)
        {
            throw new TravelGraphUnavailableException();
        }
        catch (JsonException)
        {
            throw new TravelGraphUnavailableException();
        }
        finally
        {
            _gate.Exit();
        }
    }

    private static void ValidateLocations(
        IReadOnlyList<RoutingLocation> locations)
    {
        if (locations.Any(location =>
            location is null ||
            location.LocationId == Guid.Empty ||
            location.Latitude is < -90m or > 90m ||
            location.Longitude is < -180m or > 180m))
        {
            throw new ArgumentException(
                "Locations must have valid IDs and coordinates.",
                nameof(locations));
        }

        var uniqueCount = locations
            .Select(location => location.LocationId)
            .Distinct()
            .Count();

        if (uniqueCount != locations.Count)
        {
            throw new ArgumentException(
                "Location IDs must be unique.",
                nameof(locations));
        }
    }
}