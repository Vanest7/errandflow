using System.Net.Http.Json;
using System.Text.Json;
using ErrandFlow.Application.Planning;
using ErrandFlow.Application.Planning.Dtos;
using ErrandFlow.Application.Planning.Exceptions;
using ErrandFlow.Domain.Planning;

namespace ErrandFlow.Infrastructure.Routing.Valhalla;

public sealed class ValhallaRoutePathProvider : IRoutePathProvider
{
    private readonly HttpClient _httpClient;
    private readonly ValhallaRequestGate _gate;

    public ValhallaRoutePathProvider(
        HttpClient httpClient,
        ValhallaRequestGate gate)
    {
        _httpClient = httpClient;
        _gate = gate;
    }

public async Task<RoutePathDto> BuildAsync(
    IReadOnlyList<RoutingLocation> orderedLocations,
    TravelMode travelMode,
    CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(orderedLocations);

    cancellationToken.ThrowIfCancellationRequested();

    if (travelMode != TravelMode.Walking)
    {
        throw new ArgumentException(
            "Only walking routes are currently supported.",
            nameof(travelMode));
    }

    var stops = orderedLocations.ToArray();

    ValidateStops(stops);

    if (stops.Length <= 1)
    {
        return new RoutePathDto(Array.Empty<RouteLegDto>());
    }

    const int maximumStopsPerRequest = 10;

    var legs = new List<RouteLegDto>();

    for (var offset = 0;
         offset < stops.Length - 1;
         offset += maximumStopsPerRequest - 1)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var batchStops = stops
            .Skip(offset)
            .Take(maximumStopsPerRequest)
            .ToArray();

        var batchPath = await BuildBatchAsync(
            batchStops,
            travelMode,
            cancellationToken);

        legs.AddRange(batchPath.Legs);
    }

    cancellationToken.ThrowIfCancellationRequested();

    return new RoutePathDto(legs.AsReadOnly());
}
    private async Task<RoutePathDto> BuildBatchAsync(
        IReadOnlyList<RoutingLocation> orderedLocations,
        TravelMode travelMode,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(orderedLocations);

        cancellationToken.ThrowIfCancellationRequested();

        if (travelMode != TravelMode.Walking)
        {
            throw new ArgumentException(
                "Only walking routes are currently supported.",
                nameof(travelMode));
        }

        var stops = orderedLocations.ToArray();

        ValidateStops(stops);

        if (stops.Length <= 1)
        {
            return new RoutePathDto(Array.Empty<RouteLegDto>());
        }

        var request = new
        {
            locations = stops.Select(stop => new
            {
                lat = stop.Latitude,
                lon = stop.Longitude,
                type = "break"
            }).ToArray(),
            costing = "pedestrian",
            units = "kilometers"
        };

       try
    {
        await _gate.EnterAsync(cancellationToken);
    }
    catch (TravelGraphUnavailableException)
    {
        throw new RoutePathUnavailableException();
    }

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "route",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new RoutePathUnavailableException();
            }

            var payload = await response.Content
                .ReadFromJsonAsync<ValhallaRouteResponse>(
                    cancellationToken: cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            return MapResponse(payload, stops, cancellationToken);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new RoutePathUnavailableException();
        }
        catch (HttpRequestException)
        {
            throw new RoutePathUnavailableException();
        }
        catch (JsonException)
        {
            throw new RoutePathUnavailableException();
        }
        catch (FormatException)
        {
            throw new RoutePathUnavailableException();
        }
        finally
        {
            _gate.Exit();
        }
    }

    private static RoutePathDto MapResponse(
        ValhallaRouteResponse? payload,
        IReadOnlyList<RoutingLocation> stops,
        CancellationToken cancellationToken)
    {
        var trip = payload?.Trip;

        if (trip is null ||
            trip.Status != 0 ||
            trip.Units != "kilometers" ||
            trip.Legs is null ||
            trip.Legs.Count != stops.Count - 1)
        {
            throw new RoutePathUnavailableException();
        }

        var legs = new List<RouteLegDto>(trip.Legs.Count);

        for (var index = 0; index < trip.Legs.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var source = trip.Legs[index];

            if (source?.Summary?.Length is not double kilometers ||
                source.Summary.Time is not double seconds ||
                string.IsNullOrEmpty(source.Shape))
            {
                throw new RoutePathUnavailableException();
            }

            var meters = kilometers * 1_000d;

            if (!double.IsFinite(meters) ||
                !double.IsFinite(seconds) ||
                meters < 0 ||
                seconds < 0)
            {
                throw new RoutePathUnavailableException();
            }

            var coordinates = Polyline6Decoder.Decode(
                source.Shape,
                cancellationToken);

            legs.Add(new RouteLegDto(
                stops[index].LocationId,
                stops[index + 1].LocationId,
                meters,
                seconds,
                coordinates));
        }

        return new RoutePathDto(legs.AsReadOnly());
    }

    private static void ValidateStops(
        IReadOnlyList<RoutingLocation> stops)
    {
        if (stops.Count > ScheduleOptimizer.MaximumErrands + 2)
        {
            throw new ArgumentException(
                "A route supports at most 12 stops.",
                nameof(stops));
        }

        var knownLocations = new Dictionary<Guid, RoutingLocation>();

        for (var index = 0; index < stops.Count; index++)
        {
            var stop = stops[index];

            if (stop is null ||
                stop.LocationId == Guid.Empty ||
                stop.Latitude is < -90m or > 90m ||
                stop.Longitude is < -180m or > 180m)
            {
                throw new ArgumentException(
                    "Stops must have valid IDs and coordinates.",
                    nameof(stops));
            }

            if (index > 0 &&
                stops[index - 1].LocationId == stop.LocationId)
            {
                throw new ArgumentException(
                    "Consecutive stops must have different location IDs.",
                    nameof(stops));
            }

            if (knownLocations.TryGetValue(
                    stop.LocationId,
                    out var existing) &&
                (existing.Latitude != stop.Latitude ||
                 existing.Longitude != stop.Longitude))
            {
                throw new ArgumentException(
                    "Repeated location IDs must use the same coordinates.",
                    nameof(stops));
            }

            knownLocations[stop.LocationId] = stop;
        }
    }
}