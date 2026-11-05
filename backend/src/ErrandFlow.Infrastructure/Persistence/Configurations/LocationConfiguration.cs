using ErrandFlow.Domain.Locations;
using ErrandFlow.Domain.Plans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErrandFlow.Infrastructure.Persistence.Configurations;

public sealed class LocationConfiguration
    : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("locations", table =>
        {
            table.HasCheckConstraint(
                "ck_locations_label",
                "CHAR_LENGTH(TRIM(label)) > 0");
            table.HasCheckConstraint(
                "ck_locations_latitude",
                "latitude BETWEEN -90 AND 90");
            table.HasCheckConstraint(
                "ck_locations_longitude",
                "longitude BETWEEN -180 AND 180");
        });

        builder.HasKey(location => location.Id);

        builder.Property(location => location.Id)
            .HasColumnName("id")
            .HasColumnType("char(36)")
            .HasCharSet("ascii")
            .UseCollation("ascii_bin")
            .ValueGeneratedNever();

        builder.Property(location => location.PlanId)
            .HasColumnName("plan_id")
            .HasColumnType("char(36)")
            .HasCharSet("ascii")
            .UseCollation("ascii_bin")
            .IsRequired();

        builder.Property(location => location.Label)
            .HasColumnName("label")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(location => location.Address)
            .HasColumnName("address")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(location => location.Latitude)
            .HasColumnName("latitude")
            .HasPrecision(9, 6)
            .IsRequired();

        builder.Property(location => location.Longitude)
            .HasColumnName("longitude")
            .HasPrecision(9, 6)
            .IsRequired();

        builder.HasAlternateKey(location => new
            {
                location.PlanId,
                location.Id
            })
            .HasName("uq_locations_plan_id");

        builder.HasOne<Plan>()
            .WithMany()
            .HasForeignKey(location => location.PlanId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_locations_plan");
    }
}
