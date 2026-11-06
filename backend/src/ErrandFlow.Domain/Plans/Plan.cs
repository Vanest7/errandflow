using ErrandFlow.Domain.Errands;
using ErrandFlow.Domain.Locations;

namespace ErrandFlow.Domain.Plans;

public sealed class Plan
{
    private readonly List<Errand> _errands = [];

public IReadOnlyCollection<Errand> Errands => _errands.AsReadOnly();
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

        if (planDate == default)
        {
            throw new ArgumentException(
                "Plan date must be specified.",
                nameof(planDate));
        }

        string normalizedTimeZone = timeZone.Trim();

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(normalizedTimeZone);
        }
        catch (TimeZoneNotFoundException exception)
        {
            throw new ArgumentException(
                "Time zone does not exist.",
                nameof(timeZone),
                exception);
        }
        catch (InvalidTimeZoneException exception)
        {
            throw new ArgumentException(
                "Time zone is invalid.",
                nameof(timeZone),
                exception);
        }

        Id = Guid.NewGuid();
        OwnerId = ownerId;
        Name = normalizedName;
        PlanDate = planDate;
        DepartureTime = departureTime;
        TimeZone = normalizedTimeZone;
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

        if (planDate == default)
        {
            throw new ArgumentException(
                "Plan date must be specified.",
                nameof(planDate));
        }

        string normalizedTimeZone = timeZone.Trim();

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(normalizedTimeZone);
        }
        catch (TimeZoneNotFoundException exception)
        {
            throw new ArgumentException(
                "Time zone does not exist.",
                nameof(timeZone),
                exception);
        }
        catch (InvalidTimeZoneException exception)
        {
            throw new ArgumentException(
                "Time zone is invalid.",
                nameof(timeZone),
                exception);
        }

        Name = normalizedName;
        PlanDate = planDate;
        DepartureTime = departureTime;
        TimeZone = normalizedTimeZone;
        Revision++;
        UpdatedAt = DateTime.UtcNow;
    }

    public Errand AddErrand(
    Location location,
    string title,
    ushort durationMinutes)
{
    ArgumentNullException.ThrowIfNull(location);

    if (location.PlanId != Id)
    {
        throw new ArgumentException(
            "The location must belong to this plan.",
            nameof(location));
    }

    var originalPosition = _errands.Count == 0
        ? 0u
        : checked(_errands.Max(errand => errand.OriginalPosition) + 1u);

    var errand = new Errand(
        Id,
        location.Id,
        title,
        durationMinutes,
        originalPosition);

    var nextRevision = checked(Revision + 1UL);

    _errands.Add(errand);
    Revision = nextRevision;
    UpdatedAt = DateTime.UtcNow;

    return errand;
}

public Errand? UpdateErrand(
    Guid errandId,
    Location location,
    string title,
    ushort durationMinutes)
{
    if (errandId == Guid.Empty)
    {
        throw new ArgumentException(
            "Errand ID cannot be empty.",
            nameof(errandId));
    }

    var errand = _errands.SingleOrDefault(
        item => item.Id == errandId);

    if (errand is null)
    {
        return null;
    }

    var nextRevision = checked(Revision + 1UL);

    errand.UpdateDetails(location, title, durationMinutes);

    Revision = nextRevision;
    UpdatedAt = DateTime.UtcNow;

    return errand;
}

public bool RemoveErrand(Guid errandId)
{
    if (errandId == Guid.Empty)
    {
        throw new ArgumentException(
            "Errand ID cannot be empty.",
            nameof(errandId));
    }

    var errand = _errands.SingleOrDefault(
        item => item.Id == errandId);

    if (errand is null)
    {
        return false;
    }

    var nextRevision = checked(Revision + 1UL);

    _errands.Remove(errand);
    Revision = nextRevision;
    UpdatedAt = DateTime.UtcNow;

    return true;
}

public bool RemoveLocation(Guid locationId)
{
    if (locationId == Guid.Empty)
    {
        throw new ArgumentException(
            "Location ID cannot be empty.",
            nameof(locationId));
    }

    if (OriginLocationId == locationId ||
        DestinationLocationId == locationId ||
        _errands.Any(errand => errand.LocationId == locationId))
    {
        return false;
    }

    Revision = checked(Revision + 1UL);
    UpdatedAt = DateTime.UtcNow;
    return true;
}

public void ValidateErrandOrder(IReadOnlyList<Guid> orderedErrandIds)
{
    ArgumentNullException.ThrowIfNull(orderedErrandIds);

    if (orderedErrandIds.Count != _errands.Count ||
        orderedErrandIds.Any(id => id == Guid.Empty) ||
        orderedErrandIds.Distinct().Count() != _errands.Count ||
        orderedErrandIds.Any(id => !_errands.Any(errand => errand.Id == id)))
    {
        throw new ArgumentException(
            "The order must contain each plan errand exactly once.",
            nameof(orderedErrandIds));
    }
}
public Errand? SetErrandSchedule(
    Guid errandId,
    DateTime? earliestStartAt,
    DateTime? latestFinishAt,
    DateTime? fixedStartAt)
{
    if (errandId == Guid.Empty)
    {
        throw new ArgumentException(
            "Errand ID cannot be empty.",
            nameof(errandId));
    }

    var errand = _errands.SingleOrDefault(
        item => item.Id == errandId);

    if (errand is null)
    {
        return null;
    }

    var nextRevision = checked(Revision + 1UL);

    errand.SetSchedule(
        earliestStartAt,
        latestFinishAt,
        fixedStartAt);

    Revision = nextRevision;
    UpdatedAt = DateTime.UtcNow;

    return errand;
}

public void SetLocations(
    Guid? originLocationId,
    Guid? destinationLocationId)
{
    ValidateLocationId(originLocationId, nameof(originLocationId));
    ValidateLocationId(destinationLocationId, nameof(destinationLocationId));

    OriginLocationId = originLocationId;
    DestinationLocationId = destinationLocationId;
    Revision = checked(Revision + 1UL);
    UpdatedAt = DateTime.UtcNow;
}

public Location CreateLocation(
    string label,
    string? address,
    decimal latitude,
    decimal longitude)
{
    var location = new Location(
        Id,
        label,
        address,
        latitude,
        longitude);

    var nextRevision = checked(Revision + 1UL);

    Revision = nextRevision;
    UpdatedAt = DateTime.UtcNow;

    return location;
}

public void UpdateRevision()
{
    Revision = checked(Revision + 1UL);
    UpdatedAt = DateTime.UtcNow;
}

private static void ValidateLocationId(Guid? locationId, string parameterName)
{
    if (locationId == Guid.Empty)
    {
        throw new ArgumentException(
            "Location ID cannot be empty.",
            parameterName);
    }
}
}