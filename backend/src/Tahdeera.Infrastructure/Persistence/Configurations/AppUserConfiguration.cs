using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Infrastructure.Persistence.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("app_users", t =>
        {
            t.HasCheckConstraint("ck_app_users_preferred_language", "preferred_language IN ('ar', 'en')");
            t.HasCheckConstraint("ck_app_users_preferred_theme", "preferred_theme IN ('light', 'dark')");
        });

        builder.HasKey(u => u.Id);

        builder.Property(u => u.IdentitySubject)
            .IsUnicode(false);

        builder.HasIndex(u => u.IdentitySubject)
            .IsUnique();

        builder.Property(u => u.Username)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(u => u.Username)
            .IsUnique();

        builder.Property(u => u.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.NormalizedEmail)
            .HasMaxLength(320);

        builder.HasIndex(u => u.NormalizedEmail)
            .IsUnique();

        builder.Property(u => u.Phone)
            .HasMaxLength(32);

        builder.Property(u => u.PreferredLanguage)
            .HasMaxLength(10)
            .HasDefaultValue("ar")
            .IsRequired();

        builder.Property(u => u.PreferredTheme)
            .HasMaxLength(10)
            .HasDefaultValue("light")
            .IsRequired();

        builder.Property(u => u.IsPlatformAdmin)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(u => u.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(u => u.IsActive)
            .HasDatabaseName("ix_app_users_active");
    }
}
