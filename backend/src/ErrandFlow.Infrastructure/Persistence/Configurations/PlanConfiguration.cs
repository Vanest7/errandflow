using ErrandFlow.Domain.Locations;
using ErrandFlow.Domain.Plans;
using ErrandFlow.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErrandFlow.Infrastructure.Persistence.Configurations;

public sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.Navigation(plan => plan.Errands)
            .HasField("_errands")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.ToTable("plans", table =>
        {
            table.HasCheckConstraint(
                "ck_plans_name",
                "CHAR_LENGTH(TRIM(name)) > 0");
            table.HasCheckConstraint(
                "ck_plans_zone",
                "CHAR_LENGTH(TRIM(time_zone)) > 0");
            table.HasCheckConstraint(
                "ck_plans_departure",
                "departure_time >= '00:00:00' AND departure_time < '24:00:00'");
            table.HasCheckConstraint(
                "ck_plans_revision",
                "revision > 0");
        });

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

        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(plan => new { plan.Id, plan.OriginLocationId })
            .HasPrincipalKey(location => new { location.PlanId, location.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_plans_origin");

        builder.HasIndex(plan => new { plan.Id, plan.OriginLocationId })
            .HasDatabaseName("fk_plans_origin");

        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(plan => new { plan.Id, plan.DestinationLocationId })
            .HasPrincipalKey(location => new { location.PlanId, location.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_plans_destination");

        builder.HasIndex(plan => new { plan.Id, plan.DestinationLocationId })
            .HasDatabaseName("fk_plans_destination");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(plan => plan.OwnerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_plans_owner");

        builder.HasIndex(plan => new
            {
                plan.OwnerId,
                plan.PlanDate
            })
            .HasDatabaseName("ix_plans_owner_date");
    }
}
