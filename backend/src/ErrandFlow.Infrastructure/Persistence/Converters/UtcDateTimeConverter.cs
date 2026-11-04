using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ErrandFlow.Infrastructure.Persistence.Converters;

public sealed class UtcDateTimeConverter
    : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            value => RequireUtc(value),
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }

    private static DateTime RequireUtc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new InvalidOperationException(
                "Persisted timestamps must use UTC.");
        }

        return value;
    }
}