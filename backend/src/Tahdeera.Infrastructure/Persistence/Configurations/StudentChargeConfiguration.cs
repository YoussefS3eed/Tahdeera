using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Infrastructure.Persistence.Configurations;

public class StudentChargeConfiguration : IEntityTypeConfiguration<StudentCharge>
{
    public void Configure(EntityTypeBuilder<StudentCharge> builder)
    {
        builder.ToTable("student_charges", t =>
        {
            t.HasCheckConstraint("CK_student_charges_amount_positive", "amount > 0");
            t.HasCheckConstraint("CK_student_charges_status", "status IN ('Open', 'Cancelled', 'WrittenOff')");
            t.HasCheckConstraint("CK_student_charges_cancellation_reason", "status = 'Open' OR cancellation_reason IS NOT NULL");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Description)
            .HasMaxLength(240)
            .IsRequired();

        builder.Property(x => x.BillingPeriodLabel)
            .HasMaxLength(60);

        builder.Property(x => x.Amount)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(Domain.Enums.ChargeStatus.Open)
            .IsRequired();

        builder.Property(x => x.CancellationReason);
        builder.Property(x => x.Notes);

        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        // Indexes
        builder.HasIndex(x => new { x.StudentAccountId, x.Status })
            .HasDatabaseName("ix_student_charges_account_status");

        // Foreign keys
        builder.HasOne(x => x.StudentAccount)
            .WithMany(sa => sa.Charges)
            .HasForeignKey(x => x.StudentAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Group)
            .WithMany()
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.CancelledByUser)
            .WithMany()
            .HasForeignKey(x => x.CancelledByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Allocations)
            .WithOne(a => a.Charge)
            .HasForeignKey(a => a.ChargeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
