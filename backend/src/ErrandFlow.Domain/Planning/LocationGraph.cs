namespace ErrandFlow.Domain.Planning;

public sealed class LocationGraph
{
    private readonly HashSet<Guid> _locationIds;

    private readonly Dictionary<
        (Guid From, Guid To),
        TravelCost> _costs;

    public IReadOnlyList<Guid> LocationIds { get; }

    public LocationGraph(
        IEnumerable<Guid> locationIds,
        IReadOnlyDictionary<(Guid From, Guid To), TravelCost> costs)
    {
        ArgumentNullException.ThrowIfNull(locationIds);
        ArgumentNullException.ThrowIfNull(costs);

        var ids = locationIds.ToArray();

        if (ids.Any(id => id == Guid.Empty))
        {
            throw new ArgumentException(
                "Location IDs cannot be empty.",
                nameof(locationIds));
        }

        _locationIds = ids.ToHashSet();

        if (_locationIds.Count != ids.Length)
        {
            throw new ArgumentException(
                "Location IDs must be unique.",
                nameof(locationIds));
        }

        LocationIds = Array.AsReadOnly(ids);

        _costs = costs.ToDictionary(
            entry => entry.Key,
            entry => entry.Value);

        foreach (var entry in _costs)
        {
            var (from, to) = entry.Key;

            if (!_locationIds.Contains(from) ||
                !_locationIds.Contains(to))
            {
                throw new ArgumentException(
                    "Every connection must reference graph locations.",
                    nameof(costs));
            }

            if (from == to)
            {
                throw new ArgumentException(
                    "Same-location costs are calculated automatically.",
                    nameof(costs));
            }

            if (entry.Value is null)
            {
                throw new ArgumentException(
                    "Travel costs cannot be null.",
                    nameof(costs));
            }
        }

        foreach (var from in ids)
        {
            foreach (var to in ids)
            {
                if (from != to && !_costs.ContainsKey((from, to)))
                {
                    throw new ArgumentException(
                        "Every pair of distinct locations requires a travel cost.",
                        nameof(costs));
                }
            }
        }
    }

    public TravelCost GetCost(
        Guid fromLocationId,
        Guid toLocationId)
    {
        if (!_locationIds.Contains(fromLocationId))
        {
            throw new ArgumentException(
                "The origin location does not belong to the graph.",
                nameof(fromLocationId));
        }

        if (!_locationIds.Contains(toLocationId))
        {
            throw new ArgumentException(
                "The destination location does not belong to the graph.",
                nameof(toLocationId));
        }

        if (fromLocationId == toLocationId)
        {
            return TravelCost.Zero;
        }

        return _costs[(fromLocationId, toLocationId)];
    }
}