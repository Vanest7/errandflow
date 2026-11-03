using ErrandFlow.Domain.Plans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErrandFlow.Infrastructure.Persistence.Configurations;

public sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("plans");

        builder.HasKey(plan => plan.Id);

        builder.Property(plan => plan.Id)
            .HasColumnName("id")
            .HasColumnType("char(36)")
            .HasCharSet("ascii")
            .UseCollation("ascii_bin")
            .ValueGeneratedNever();

        builder.Property(plan => plan.OwnerId)
            .HasColumnName("owner_id")
            .HasColumnType("char(36)")
            .HasCharSet("ascii")
            .UseCollation("ascii_bin")
            .IsRequired();

        builder.Property(plan => plan.Name)
            .HasColumnName("name")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(plan => plan.PlanDate)
            .HasColumnName("plan_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(plan => plan.DepartureTime)
            .HasColumnName("departure_time")
            .HasColumnType("time")
            .IsRequired();

        builder.Property(plan => plan.TimeZone)
            .HasColumnName("time_zone")
            .HasMaxLength(100)
            .HasDefaultValue("Europe/Madrid")
            .IsRequired();

        builder.Property(plan => plan.OriginLocationId)
            .HasColumnName("origin_location_id")
            .HasColumnType("char(36)")
            .HasCharSet("ascii")
            .UseCollation("ascii_bin");

        builder.Property(plan => plan.DestinationLocationId)
            .HasColumnName("destination_location_id")
            .HasColumnType("char(36)")
            .HasCharSet("ascii")
            .UseCollation("ascii_bin");

        builder.Property(plan => plan.Revision)
            .HasColumnName("revision")
            .HasColumnType("bigint unsigned")
            .HasDefaultValue(1UL)
            .IsConcurrencyToken()
            .IsRequired();

        builder.Property(plan => plan.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime(6)")
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(plan => plan.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("datetime(6)")
            .ValueGeneratedNever()
            .IsRequired();

        builder.HasIndex(plan => new
            {
                plan.OwnerId,
                plan.PlanDate
            })
            .HasDatabaseName("ix_plans_owner_date");
    }
}