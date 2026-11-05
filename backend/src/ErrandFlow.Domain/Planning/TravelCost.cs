namespace ErrandFlow.Domain.Planning;

public sealed record TravelCost
{
    public bool IsReachable { get; }

    public TimeSpan? Duration { get; }

    public decimal? DistanceMeters { get; }

    private TravelCost(
        bool isReachable,
        TimeSpan? duration,
        decimal? distanceMeters)
    {
        IsReachable = isReachable;
        Duration = duration;
        DistanceMeters = distanceMeters;
    }

    public static TravelCost Unreachable { get; } =
        new(false, null, null);

    public static TravelCost Zero { get; } =
        new(true, TimeSpan.Zero, 0m);

    public static TravelCost Create(
        TimeSpan duration,
        decimal distanceMeters)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(duration),
                "Travel duration cannot be negative.");
        }

        if (distanceMeters < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(distanceMeters),
                "Travel distance cannot be negative.");
        }

        return new TravelCost(
            true,
            duration,
            distanceMeters);
    }
}