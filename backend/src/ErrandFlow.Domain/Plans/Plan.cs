namespace ErrandFlow.Domain.Plans;

public sealed class Plan
{
    public Guid Id { get; private set; }

    public Guid OwnerId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public DateOnly PlanDate { get; private set; }

    public TimeOnly DepartureTime { get; private set; }

    public string TimeZone { get; private set; } = "Europe/Madrid";

    public Guid? OriginLocationId { get; private set; }

    public Guid? DestinationLocationId { get; private set; }

    public ulong Revision { get; private set; } = 1;

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    private Plan()
    {
    }

    public Plan(
        Guid ownerId,
        string name,
        DateOnly planDate,
        TimeOnly departureTime,
        string timeZone = "Europe/Madrid")
    {
        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Owner ID cannot be empty.",
                nameof(ownerId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Plan name cannot be empty.",
                nameof(name));
        }

        string normalizedName = name.Trim();

        if (normalizedName.Length > 150)
        {
            throw new ArgumentException(
                "Plan name cannot exceed 150 characters.",
                nameof(name));
        }

        if (string.IsNullOrWhiteSpace(timeZone) ||
            timeZone.Trim().Length > 100)
        {
            throw new ArgumentException(
                "Invalid time zone.",
                nameof(timeZone));
        }

        Id = Guid.NewGuid();
        OwnerId = ownerId;
        Name = normalizedName;
        PlanDate = planDate;
        DepartureTime = departureTime;
        TimeZone = timeZone.Trim();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public void UpdateDetails(
        string name,
        DateOnly planDate,
        TimeOnly departureTime,
        string timeZone)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Plan name cannot be empty.",
                nameof(name));
        }

        string normalizedName = name.Trim();

        if (normalizedName.Length > 150)
        {
            throw new ArgumentException(
                "Plan name cannot exceed 150 characters.",
                nameof(name));
        }

        if (string.IsNullOrWhiteSpace(timeZone) ||
            timeZone.Trim().Length > 100)
        {
            throw new ArgumentException(
                "Invalid time zone.",
                nameof(timeZone));
        }

        Name = normalizedName;
        PlanDate = planDate;
        DepartureTime = departureTime;
        TimeZone = timeZone.Trim();
        Revision++;
        UpdatedAt = DateTime.UtcNow;
    }
}