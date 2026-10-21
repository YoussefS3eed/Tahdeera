using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Infrastructure.Persistence.Configurations;

public class WorkspaceWhatsAppUsageLedgerConfiguration : IEntityTypeConfiguration<WorkspaceWhatsAppUsageLedger>
{
    public void Configure(EntityTypeBuilder<WorkspaceWhatsAppUsageLedger> builder)
    {
        builder.ToTable("workspace_whatsapp_usage_ledgers", t =>
        {
            t.HasCheckConstraint("chk_whatsapp_usage_year_month", "year_month ~ '^[0-9]{4}-(0[1-9]|1[0-2])$'");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.WorkspaceId)
            .IsRequired();

        builder.Property(x => x.YearMonth)
            .HasMaxLength(7)
            .IsRequired();

        builder.Property(x => x.MessagesDispatchedCount)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.MessagesDeliveredCount)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.MessagesFailedCount)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.EstimatedCostUsd)
            .HasPrecision(10, 4)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.EstimatedCostEgp)
            .HasPrecision(10, 4)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(x => x.RateCardSnapshotJson)
            .HasColumnType("jsonb");

        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(x => new { x.WorkspaceId, x.YearMonth })
            .IsUnique()
            .HasDatabaseName("ux_whatsapp_usage_workspace_month");

        builder.HasIndex(x => new { x.WorkspaceId, x.YearMonth })
            .HasDatabaseName("ix_whatsapp_usage_workspace_month");

        builder.HasOne(x => x.Workspace)
            .WithMany()
            .HasForeignKey(x => x.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
