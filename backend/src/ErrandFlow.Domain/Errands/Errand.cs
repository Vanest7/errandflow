using ErrandFlow.Domain.Locations;

namespace ErrandFlow.Domain.Errands;

public sealed class Errand
{
    public Guid Id { get; private set; }

    public Guid PlanId { get; private set; }

    public Guid LocationId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public ushort DurationMinutes { get; private set; }

    public uint OriginalPosition { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public DateTime? EarliestStartAt { get; private set; }

    public DateTime? LatestFinishAt { get; private set; }

    public DateTime? FixedStartAt { get; private set; }
    private Errand()
    {
    }

    internal Errand(
        Guid planId,
        Guid locationId,
        string title,
        ushort durationMinutes,
        uint originalPosition)
    {
        if (planId == Guid.Empty)
        {
            throw new ArgumentException(
                "Plan ID cannot be empty.",
                nameof(planId));
        }

        if (locationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Location ID cannot be empty.",
                nameof(locationId));
        }

        var normalizedTitle = NormalizeTitle(title);

        if (durationMinutes == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(durationMinutes),
                "Duration must be greater than zero minutes.");
        }

        Id = Guid.NewGuid();
        PlanId = planId;
        LocationId = locationId;
        Title = normalizedTitle;
        DurationMinutes = durationMinutes;
        OriginalPosition = originalPosition;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "Errand title is required.",
                nameof(title));
        }

        var normalizedTitle = title.Trim();

        if (normalizedTitle.Length > 150)
        {
            throw new ArgumentException(
                "Errand title cannot exceed 150 characters.",
                nameof(title));
        }

        return normalizedTitle;
    }
    internal void UpdateDetails(
    Location location,
    string title,
    ushort durationMinutes)
{
    ArgumentNullException.ThrowIfNull(location);

    if (location.PlanId != PlanId)
    {
        throw new ArgumentException(
            "The location must belong to the errand's plan.",
            nameof(location));
    }

    var normalizedTitle = NormalizeTitle(title);

    if (durationMinutes == 0)
    {
        throw new ArgumentOutOfRangeException(
            nameof(durationMinutes),
            "Duration must be greater than zero minutes.");
    }
    ValidateSchedule(
        durationMinutes,
        EarliestStartAt,
        LatestFinishAt,
        FixedStartAt);
        LocationId = location.Id;
        Title = normalizedTitle;
        DurationMinutes = durationMinutes;
        UpdatedAt = DateTime.UtcNow;
    }
private static void ValidateUtc(DateTime? value, string parameterName)
{
    if (value.HasValue && value.Value.Kind != DateTimeKind.Utc)
    {
        throw new ArgumentException(
            "Schedule timestamps must use UTC.",
            parameterName);
    }
}

private static void ValidateSchedule(
    ushort durationMinutes,
    DateTime? earliestStartAt,
    DateTime? latestFinishAt,
    DateTime? fixedStartAt)
{
    ValidateUtc(earliestStartAt, nameof(earliestStartAt));
    ValidateUtc(latestFinishAt, nameof(latestFinishAt));
    ValidateUtc(fixedStartAt, nameof(fixedStartAt));

    if (earliestStartAt.HasValue &&
        latestFinishAt.HasValue &&
        earliestStartAt.Value >= latestFinishAt.Value)
    {
        throw new ArgumentException(
            "Latest finish must be after earliest start.",
            nameof(latestFinishAt));
    }

    if (fixedStartAt.HasValue &&
        earliestStartAt.HasValue &&
        fixedStartAt.Value < earliestStartAt.Value)
    {
        throw new ArgumentException(
            "Fixed start cannot be before earliest start.",
            nameof(fixedStartAt));
    }

    var minimumStartAt = fixedStartAt ?? earliestStartAt;

    if (minimumStartAt.HasValue && latestFinishAt.HasValue)
    {
        var availableTime =
            latestFinishAt.Value - minimumStartAt.Value;

        var requiredTime =
            TimeSpan.FromMinutes(durationMinutes);

        if (availableTime < requiredTime)
        {
            throw new ArgumentException(
                "The errand must finish within the allowed time window.",
                nameof(latestFinishAt));
        }
    }
}
internal void SetSchedule(
    DateTime? earliestStartAt,
    DateTime? latestFinishAt,
    DateTime? fixedStartAt)
{
    ValidateSchedule(
        DurationMinutes,
        earliestStartAt,
        latestFinishAt,
        fixedStartAt);

    EarliestStartAt = earliestStartAt;
    LatestFinishAt = latestFinishAt;
    FixedStartAt = fixedStartAt;
    UpdatedAt = DateTime.UtcNow;
}
}