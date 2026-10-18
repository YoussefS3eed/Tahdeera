using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.ValueObjects;

namespace Tahdeera.Infrastructure.Persistence.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("subscription_plans", t =>
        {
            t.HasCheckConstraint("ck_subscription_plans_default_price", "default_price >= 0");
            t.HasCheckConstraint("ck_subscription_plans_duration_days", "duration_days > 0");
            t.HasCheckConstraint("ck_subscription_plans_max_students", "max_students > 0");
            t.HasCheckConstraint("ck_subscription_plans_max_staff", "max_staff > 0");
            t.HasCheckConstraint("ck_subscription_plans_max_branches", "max_branches > 0");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code)
            .HasMaxLength(40)
            .IsRequired();

        builder.HasIndex(p => p.Code)
            .IsUnique();

        builder.Property(p => p.NameAr)
            .HasMaxLength(120)
            .IsRequired();

        builder.HasIndex(p => p.NameAr)
            .IsUnique();

        builder.Property(p => p.NameEn)
            .HasMaxLength(120)
            .IsRequired();

        builder.HasIndex(p => p.NameEn)
            .IsUnique();

        builder.Property(p => p.DescriptionAr)
            .HasColumnType("text");

        builder.Property(p => p.DescriptionEn)
            .HasColumnType("text");

        builder.Property(p => p.DefaultPrice)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(p => p.Currency)
            .HasMaxLength(3)
            .HasDefaultValue("EGP")
            .IsRequired();

        builder.Property(p => p.DurationDays)
            .HasDefaultValue(365)
            .IsRequired();

        builder.Property(p => p.MaxStudents)
            .HasDefaultValue(150)
            .IsRequired();

        builder.Property(p => p.MaxStaff)
            .HasDefaultValue(1)
            .IsRequired();

        builder.Property(p => p.MaxBranches)
            .HasDefaultValue(1)
            .IsRequired();

        builder.Property(p => p.Features)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<SubscriptionPlanFeatures>(v, JsonOptions) ?? new SubscriptionPlanFeatures(true, true, true, true, false, false, true, false, true, false))
            .IsRequired();

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true)
            .IsRequired();
    }
}
