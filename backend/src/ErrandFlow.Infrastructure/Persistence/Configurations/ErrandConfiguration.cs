using ErrandFlow.Domain.Errands;
using ErrandFlow.Domain.Locations;
using ErrandFlow.Domain.Plans;
using ErrandFlow.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErrandFlow.Infrastructure.Persistence.Configurations;

public sealed class ErrandConfiguration
    : IEntityTypeConfiguration<Errand>
{
    public void Configure(EntityTypeBuilder<Errand> builder)
    {
        var utcConverter = new UtcDateTimeConverter();

        builder.ToTable("errands");

        builder.HasKey(errand => errand.Id);

        builder.Property(errand => errand.Id)
            .HasColumnName("id")
            .HasColumnType("char(36)")
            .HasCharSet("ascii")
            .UseCollation("ascii_bin")
            .ValueGeneratedNever();

        builder.Property(errand => errand.PlanId)
            .HasColumnName("plan_id")
            .HasColumnType("char(36)")
            .HasCharSet("ascii")
            .UseCollation("ascii_bin")
            .IsRequired();

        builder.Property(errand => errand.LocationId)
            .HasColumnName("location_id")
            .HasColumnType("char(36)")
            .HasCharSet("ascii")
            .UseCollation("ascii_bin")
            .IsRequired();

        builder.Property(errand => errand.Title)
            .HasColumnName("title")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(errand => errand.DurationMinutes)
            .HasColumnName("duration_minutes")
            .HasColumnType("smallint unsigned")
            .IsRequired();

        builder.Property(errand => errand.OriginalPosition)
            .HasColumnName("original_position")
            .HasColumnType("int unsigned")
            .IsRequired();

        builder.Property(errand => errand.EarliestStartAt)
            .HasColumnName("earliest_start_at")
            .HasColumnType("datetime(6)")
            .HasConversion(utcConverter);

        builder.Property(errand => errand.LatestFinishAt)
            .HasColumnName("latest_finish_at")
            .HasColumnType("datetime(6)")
            .HasConversion(utcConverter);

        builder.Property(errand => errand.FixedStartAt)
            .HasColumnName("fixed_start_at")
            .HasColumnType("datetime(6)")
            .HasConversion(utcConverter);

        builder.Property(errand => errand.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime(6)")
            .HasConversion(utcConverter)
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(errand => errand.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("datetime(6)")
            .HasConversion(utcConverter)
            .ValueGeneratedNever()
            .IsRequired();

        builder.HasIndex(errand => new
            {
                errand.PlanId,
                errand.OriginalPosition
            })
            .IsUnique()
            .HasDatabaseName("uq_errands_position");

        builder.HasIndex(errand => new
            {
                errand.PlanId,
                errand.LocationId
            })
            .HasDatabaseName("ix_errands_location");

        builder.HasOne<Plan>()
            .WithMany(plan => plan.Errands)
            .HasForeignKey(errand => errand.PlanId)
            .OnDelete(DeleteBehavior.ClientCascade)
            .HasConstraintName("fk_errands_plan");

        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(errand => new
            {
                errand.PlanId,
                errand.LocationId
            })
            .HasPrincipalKey(location => new
            {
                location.PlanId,
                location.Id
            })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_errands_location");
    }
}