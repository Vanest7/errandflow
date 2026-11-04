using ErrandFlow.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErrandFlow.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .HasColumnName("id")
            .HasColumnType("char(36)")
            .HasCharSet("ascii")
            .UseCollation("ascii_bin")
            .ValueGeneratedNever();

        builder.Property(user => user.IdentityUserId)
            .HasColumnName("identity_user_id")
            .HasMaxLength(255)
            .HasCharSet("ascii")
            .UseCollation("ascii_bin");

        builder.HasIndex(user => user.IdentityUserId)
            .IsUnique()
            .HasDatabaseName("uq_users_identity");

        builder.Property(user => user.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(user => user.PreferredLanguage)
            .HasColumnName("preferred_language")
            .HasMaxLength(5)
            .HasDefaultValue("es")
            .IsRequired();

        builder.Property(user => user.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime(6)")
            .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
            .ValueGeneratedOnAdd();
    }
}