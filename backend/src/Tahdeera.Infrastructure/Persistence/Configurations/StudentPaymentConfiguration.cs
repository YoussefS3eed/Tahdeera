using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Infrastructure.Persistence.Configurations;

public class StudentPaymentConfiguration : IEntityTypeConfiguration<StudentPayment>
{
    public void Configure(EntityTypeBuilder<StudentPayment> builder)
    {
        builder.ToTable("student_payments", t =>
        {
            t.HasCheckConstraint("CK_student_payments_amount_positive", "amount > 0");
            t.HasCheckConstraint("CK_student_payments_method", "method IN ('Cash', 'BankTransfer', 'Wallet', 'Card', 'Other')");
            t.HasCheckConstraint("CK_student_payments_status", "status IN ('Received', 'Reversed')");
            t.HasCheckConstraint("CK_student_payments_reversal_validity", "(status = 'Received' AND reversed_at IS NULL) OR (status = 'Reversed' AND reversed_at IS NOT NULL AND reversal_reason IS NOT NULL)");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.ReceiptNumber)
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(x => x.PaidAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(x => x.Method)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Reference)
            .HasMaxLength(160);

        builder.Property(x => x.Notes);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(PaymentStatus.Received)
            .IsRequired();

        builder.Property(x => x.ReversalReason);

        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        // Indexes
        builder.HasIndex(x => x.ReceiptNumber)
            .IsUnique()
            .HasDatabaseName("ux_student_payments_receipt_number");

        builder.HasIndex(x => new { x.StudentAccountId, x.PaidAt })
            .HasDatabaseName("ix_student_payments_account_date");

        // Foreign keys
        builder.HasOne(x => x.StudentAccount)
            .WithMany(sa => sa.Payments)
            .HasForeignKey(x => x.StudentAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RecordedByUser)
            .WithMany()
            .HasForeignKey(x => x.RecordedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReversedByUser)
            .WithMany()
            .HasForeignKey(x => x.ReversedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.ReversesPayment)
            .WithMany()
            .HasForeignKey(x => x.ReversesPaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Allocations)
            .WithOne(a => a.Payment)
            .HasForeignKey(a => a.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
