namespace ErrandFlow.Domain.Locations;

public sealed class Location
{
    public Guid Id { get; private set; }

    public Guid PlanId { get; private set; }

    public string Label { get; private set; } = string.Empty;

    public string? Address { get; private set; }

    public decimal Latitude { get; private set; }

    public decimal Longitude { get; private set; }

    private Location()
    {
    }

    public Location(
        Guid planId,
        string label,
        string? address,
        decimal latitude,
        decimal longitude)
    {
        if (planId == Guid.Empty)
        {
            throw new ArgumentException(
                "Plan ID cannot be empty.",
                nameof(planId));
        }

        var normalizedLabel = NormalizeLabel(label);
        var normalizedAddress = NormalizeAddress(address);

        ValidateCoordinates(latitude, longitude);

        Id = Guid.NewGuid();
        PlanId = planId;
        Label = normalizedLabel;
        Address = normalizedAddress;
        Latitude = latitude;
        Longitude = longitude;
    }

    public void UpdateDetails(
        string label,
        string? address,
        decimal latitude,
        decimal longitude)
    {
        var normalizedLabel = NormalizeLabel(label);
        var normalizedAddress = NormalizeAddress(address);
        ValidateCoordinates(latitude, longitude);
        Label = normalizedLabel;
        Address = normalizedAddress;
        Latitude = latitude;
        Longitude = longitude;
    }

    private static string NormalizeLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException(
                "Location label is required.",
                nameof(label));
        }

        var normalizedLabel = label.Trim();

        if (normalizedLabel.Length > 150)
        {
            throw new ArgumentException(
                "Location label cannot exceed 150 characters.",
                nameof(label));
        }

        return normalizedLabel;
    }

    private static string? NormalizeAddress(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        var normalizedAddress = address.Trim();

        if (normalizedAddress.Length > 500)
        {
            throw new ArgumentException(
                "Location address cannot exceed 500 characters.",
                nameof(address));
        }

        return normalizedAddress;
    }

    private static void ValidateCoordinates(
        decimal latitude,
        decimal longitude)
    {
        if (latitude < -90m || latitude > 90m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(latitude),
                "Latitude must be between -90 and 90 degrees.");
        }

        if (longitude < -180m || longitude > 180m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(longitude),
                "Longitude must be between -180 and 180 degrees.");
        }
    }
}