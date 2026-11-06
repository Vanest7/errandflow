namespace ErrandFlow.Infrastructure.Routing.Valhalla;

public sealed class ValhallaRouteResponse
{
    public ValhallaRouteTrip? Trip { get; init; }
}

public sealed class ValhallaRouteTrip
{
    public int? Status { get; init; }

    public string? Units { get; init; }

    public List<ValhallaRouteLeg>? Legs { get; init; }
}

public sealed class ValhallaRouteLeg
{
    public ValhallaRouteSummary? Summary { get; init; }

    public string? Shape { get; init; }
}

public sealed class ValhallaRouteSummary
{
    public double? Length { get; init; }

    public double? Time { get; init; }
}