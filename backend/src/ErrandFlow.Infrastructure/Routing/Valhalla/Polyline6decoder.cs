using ErrandFlow.Application.Planning.Dtos;

namespace ErrandFlow.Infrastructure.Routing.Valhalla;

internal static class Polyline6Decoder
{
    public static IReadOnlyList<RouteCoordinate> Decode(
        string encoded,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(encoded))
        {
            throw new FormatException("Route geometry is missing.");
        }

        var coordinates = new List<RouteCoordinate>();
        var index = 0;
        long latitude = 0;
        long longitude = 0;

        while (index < encoded.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();

            latitude += ReadDelta(encoded, ref index);
            longitude += ReadDelta(encoded, ref index);

            var latitudeDegrees = latitude / 1_000_000m;
            var longitudeDegrees = longitude / 1_000_000m;

            if (latitudeDegrees is < -90m or > 90m ||
                longitudeDegrees is < -180m or > 180m)
            {
                throw new FormatException(
                    "Route geometry contains invalid coordinates.");
            }

            if (coordinates.Count >= 100_000)
            {
                throw new FormatException(
                    "Route geometry contains too many coordinates.");
            }

            coordinates.Add(new RouteCoordinate(
                latitudeDegrees,
                longitudeDegrees));
        }

        return coordinates.AsReadOnly();
    }

    private static long ReadDelta(string encoded, ref int index)
    {
        long value = 0;
        var shift = 0;

        for (var group = 0; group < 7; group++)
        {
            if (index >= encoded.Length)
            {
                throw new FormatException(
                    "Route geometry is incomplete.");
            }

            var chunk = encoded[index++] - 63;

            if (chunk is < 0 or > 63)
            {
                throw new FormatException(
                    "Route geometry contains an invalid character.");
            }

            value |= (long)(chunk & 31) << shift;

            if ((chunk & 32) == 0)
            {
                return (value & 1) == 0
                    ? value >> 1
                    : ~(value >> 1);
            }

            shift += 5;
        }

        throw new FormatException(
            "Route geometry contains an oversized coordinate.");
    }
}