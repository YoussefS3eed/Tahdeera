using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Infrastructure.Persistence.Configurations;

public class SubscriptionPaymentConfiguration : IEntityTypeConfiguration<SubscriptionPayment>
{
    public void Configure(EntityTypeBuilder<SubscriptionPayment> builder)
    {
        builder.ToTable("subscription_payments", t =>
        {
            t.HasCheckConstraint("ck_subscription_payments_amount", "amount > 0");
            t.HasCheckConstraint("ck_subscription_payments_method", "method IN ('Cash', 'BankTransfer', 'Wallet', 'Card', 'Other')");
            t.HasCheckConstraint("ck_subscription_payments_status", "status IN ('Received', 'Reversed')");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(p => p.PaidAt)
            .IsRequired();

        builder.Property(p => p.Method)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(p => p.Reference)
            .HasMaxLength(160);

        builder.Property(p => p.Status)
            .HasMaxLength(20)
            .HasDefaultValue("Received")
            .IsRequired();

        builder.Property(p => p.ReversalReason)
            .HasColumnType("text");

        builder.Property(p => p.Notes)
            .HasColumnType("text");

        builder.HasOne(p => p.Subscription)
            .WithMany(s => s.Payments)
            .HasForeignKey(p => p.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.RecordedByUser)
            .WithMany()
            .HasForeignKey(p => p.RecordedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
