using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Infrastructure.Persistence.Configurations;

public class WorkspaceReceiptSequenceConfiguration : IEntityTypeConfiguration<WorkspaceReceiptSequence>
{
    public void Configure(EntityTypeBuilder<WorkspaceReceiptSequence> builder)
    {
        builder.ToTable("workspace_receipt_sequences");

        builder.HasKey(x => new { x.WorkspaceId, x.YearMonth });

        builder.Property(x => x.NextSequence)
            .HasDefaultValue(1)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasOne(x => x.Workspace)
            .WithMany()
            .HasForeignKey(x => x.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
