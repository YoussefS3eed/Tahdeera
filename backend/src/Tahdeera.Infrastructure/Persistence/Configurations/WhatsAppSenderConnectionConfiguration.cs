using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Infrastructure.Persistence.Configurations;

public class WhatsAppSenderConnectionConfiguration : IEntityTypeConfiguration<WhatsAppSenderConnection>
{
    public void Configure(EntityTypeBuilder<WhatsAppSenderConnection> builder)
    {
        builder.ToTable("whatsapp_sender_connections", t =>
        {
            t.HasCheckConstraint("chk_whatsapp_sender_ownership", "ownership_type IN ('Platform', 'Workspace')");
            t.HasCheckConstraint("chk_whatsapp_sender_status", "status IN ('PendingSetup', 'Connected', 'Disconnected', 'Restricted', 'Failed')");
            t.HasCheckConstraint("chk_whatsapp_sender_phone", "display_phone_number ~ '^\\+?[0-9\\s\\-]+$'");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.WorkspaceId)
            .IsRequired();

        builder.Property(x => x.OwnershipType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.WabaId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.PhoneNumberId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.DisplayPhoneNumber)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.VerifiedBusinessName)
            .HasMaxLength(200);

        builder.Property(x => x.EncryptedAccessToken)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.ConnectedAt)
            .HasDefaultValueSql("now()");

        builder.Property(x => x.DisconnectedAt);

        builder.Property(x => x.RevocationReason)
            .HasMaxLength(255);

        builder.Property(x => x.MetadataJson)
            .HasColumnType("jsonb");

        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(x => x.WorkspaceId)
            .HasDatabaseName("ix_whatsapp_sender_workspace");

        builder.HasIndex(x => x.WorkspaceId)
            .HasDatabaseName("ix_whatsapp_sender_active")
            .HasFilter("status = 'Connected'");

        builder.HasOne(x => x.Workspace)
            .WithMany()
            .HasForeignKey(x => x.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
