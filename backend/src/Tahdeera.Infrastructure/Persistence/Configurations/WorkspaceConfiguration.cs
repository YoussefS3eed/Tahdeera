using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Infrastructure.Persistence.Configurations;

public class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable("workspaces", t =>
        {
            t.HasCheckConstraint("ck_workspaces_kind", "kind IN ('Teacher', 'Center')");
            t.HasCheckConstraint("ck_workspaces_late_grace", "default_late_grace_minutes >= 0 AND default_late_grace_minutes <= 1440");
        });

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(w => w.Kind)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(w => w.TimezoneId)
            .HasMaxLength(100)
            .HasDefaultValue("Africa/Cairo")
            .IsRequired();

        builder.Property(w => w.DefaultLateGraceMinutes)
            .HasDefaultValue(20)
            .IsRequired();

        builder.Property(w => w.WeeklyHolidayDays)
            .HasColumnType("smallint[]")
            .IsRequired();

        builder.Property(w => w.ContactPhone)
            .HasMaxLength(11);

        builder.Property(w => w.CityAddress)
            .HasMaxLength(240);

        builder.Property(w => w.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(w => w.IsActive)
            .HasDatabaseName("ix_workspaces_active");

        builder.HasOne(w => w.CreatedByUser)
            .WithMany()
            .HasForeignKey(w => w.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
