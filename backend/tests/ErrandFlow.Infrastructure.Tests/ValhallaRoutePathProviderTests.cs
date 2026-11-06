using System.Net;
using System.Text;
using System.Text.Json;
using ErrandFlow.Application.Planning;
using ErrandFlow.Application.Planning.Dtos;
using ErrandFlow.Application.Planning.Exceptions;
using ErrandFlow.Infrastructure.Routing.Valhalla;
using Xunit;

namespace ErrandFlow.Infrastructure.Tests;

public sealed class ValhallaRoutePathProviderTests
{
    private const string ValidShape =
        "_p~iF~ps|U_ulLnnqC_mqNvxq`@";

    [Fact]
    public async Task BuildAsync_SendsOrderedStopsAndMapsRoute()
    {
        var stops = CreateStops();

        using var handler = new StubHandler(CreatePayload());
        using var client = CreateClient(handler);
        using var gate = new ValhallaRequestGate();

        var provider = new ValhallaRoutePathProvider(client, gate);

        var result = await provider.BuildAsync(
            stops,
            TravelMode.Walking);

        Assert.Equal(1, handler.Calls);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("/route", handler.RequestUri!.AbsolutePath);

        using var request = JsonDocument.Parse(handler.Body!);
        var root = request.RootElement;

        Assert.Equal(
            "pedestrian",
            root.GetProperty("costing").GetString());

        Assert.Equal(
            "kilometers",
            root.GetProperty("units").GetString());

        var sentLocations = root.GetProperty("locations");

        Assert.Equal(2, sentLocations.GetArrayLength());

        for (var index = 0; index < stops.Length; index++)
        {
            var sent = sentLocations[index];

            Assert.Equal(
                stops[index].Latitude,
                sent.GetProperty("lat").GetDecimal());

            Assert.Equal(
                stops[index].Longitude,
                sent.GetProperty("lon").GetDecimal());

            Assert.Equal(
                "break",
                sent.GetProperty("type").GetString());
        }

        var leg = Assert.Single(result.Legs);

        Assert.Equal(stops[0].LocationId, leg.FromLocationId);
        Assert.Equal(stops[1].LocationId, leg.ToLocationId);
        Assert.Equal(1250d, leg.DistanceMeters);
        Assert.Equal(300d, leg.TravelDurationSeconds);

        Assert.Equal(
            new[]
            {
                new RouteCoordinate(3.85m, -12.02m),
                new RouteCoordinate(4.07m, -12.095m),
                new RouteCoordinate(4.3252m, -12.6453m)
            },
            leg.Coordinates.ToArray());

        Assert.Equal(1250d, result.TotalDistanceMeters);
        Assert.Equal(300d, result.TotalTravelDurationSeconds);
    }

    [Theory]
    [MemberData(nameof(InvalidResponses))]
    public async Task BuildAsync_RejectsInvalidProviderResponse(
        string body)
    {
        using var handler = new StubHandler(body);
        using var client = CreateClient(handler);
        using var gate = new ValhallaRequestGate();

        var provider = new ValhallaRoutePathProvider(client, gate);

        await Assert.ThrowsAsync<RoutePathUnavailableException>(
            () => provider.BuildAsync(
                CreateStops(),
                TravelMode.Walking));
    }

    public static IEnumerable<object[]> InvalidResponses()
    {
        yield return new object[] { "{}" };
        yield return new object[] { "not-json" };
        yield return new object[] { CreatePayload(shape: "?") };
        yield return new object[] { CreatePayload(units: "miles") };
        yield return new object[] { CreatePayload(length: -1d) };
        yield return new object[] { CreatePayload(status: 1) };
    }

    [Theory]
    [InlineData(429)]
    [InlineData(503)]
    public async Task BuildAsync_TranslatesHttpFailure(int statusCode)
    {
        using var handler = new StubHandler(
            "{}",
            (HttpStatusCode)statusCode);

        using var client = CreateClient(handler);
        using var gate = new ValhallaRequestGate();

        var provider = new ValhallaRoutePathProvider(client, gate);

        await Assert.ThrowsAsync<RoutePathUnavailableException>(
            () => provider.BuildAsync(
                CreateStops(),
                TravelMode.Walking));
    }

    [Fact]
    public async Task BuildAsync_SkipsHttpForSingleLocation()
    {
        using var handler = new StubHandler("{}");
        using var client = CreateClient(handler);
        using var gate = new ValhallaRequestGate();

        var provider = new ValhallaRoutePathProvider(client, gate);

        var result = await provider.BuildAsync(
            new[] { CreateStops()[0] },
            TravelMode.Walking);

        Assert.Empty(result.Legs);
        Assert.Equal(0d, result.TotalDistanceMeters);
        Assert.Equal(0d, result.TotalTravelDurationSeconds);
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task BuildAsync_PreservesCallerCancellation()
    {
        using var handler = new StubHandler(CreatePayload());
        using var client = CreateClient(handler);
        using var gate = new ValhallaRequestGate();
        using var cancellation = new CancellationTokenSource();

        cancellation.Cancel();

        var provider = new ValhallaRoutePathProvider(client, gate);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => provider.BuildAsync(
                CreateStops(),
                TravelMode.Walking,
                cancellation.Token));

        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task BuildAsync_TranslatesBusyGateFailure()
    {
        using var handler = new StubHandler(CreatePayload());
        using var client = CreateClient(handler);
        using var gate = new ValhallaRequestGate();

        await gate.EnterAsync(CancellationToken.None);

        try
        {
            var provider = new ValhallaRoutePathProvider(client, gate);

            await Assert.ThrowsAsync<RoutePathUnavailableException>(
                () => provider.BuildAsync(
                    CreateStops(),
                    TravelMode.Walking));

            Assert.Equal(0, handler.Calls);
        }
        finally
        {
            gate.Exit();
        }
    }

    private static RoutingLocation[] CreateStops()
    {
        return new[]
        {
            new RoutingLocation(
                Guid.NewGuid(), 3.85m, -12.02m),
            new RoutingLocation(
                Guid.NewGuid(), 4.3252m, -12.6453m)
        };
    }

    private static string CreatePayload(
        string shape = ValidShape,
        string units = "kilometers",
        double length = 1.25d,
        int status = 0)
    {
        return JsonSerializer.Serialize(new
        {
            trip = new
            {
                status,
                units,
                legs = new[]
                {
                    new
                    {
                        summary = new
                        {
                            length,
                            time = 300d
                        },
                        shape
                    }
                }
            }
        });
    }

    private static HttpClient CreateClient(HttpMessageHandler handler)
    {
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://routing.example/")
        };
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _body;
        private readonly HttpStatusCode _statusCode;

        public int Calls { get; private set; }
        public HttpMethod? Method { get; private set; }
        public Uri? RequestUri { get; private set; }
        public string? Body { get; private set; }

        public StubHandler(
            string body,
            HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _body = body;
            _statusCode = statusCode;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Calls++;
            Method = request.Method;
            RequestUri = request.RequestUri;

            Body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(
                    cancellationToken);

            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(
                    _body,
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }
}