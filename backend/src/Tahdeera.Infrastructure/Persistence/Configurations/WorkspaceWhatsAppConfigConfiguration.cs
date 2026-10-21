using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Infrastructure.Persistence.Configurations;

public class WorkspaceWhatsAppConfigConfiguration : IEntityTypeConfiguration<WorkspaceWhatsAppConfig>
{
    public void Configure(EntityTypeBuilder<WorkspaceWhatsAppConfig> builder)
    {
        builder.ToTable("workspace_whatsapp_configs", t =>
        {
            t.HasCheckConstraint("chk_whatsapp_sender_mode", "sender_mode IN ('PlatformManaged', 'WorkspaceOwned', 'Disabled')");
            t.HasCheckConstraint("chk_whatsapp_monthly_quota", "monthly_message_quota >= 0");
            t.HasCheckConstraint("chk_whatsapp_used_messages", "messages_used_this_month >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.WorkspaceId)
            .IsRequired();

        builder.Property(x => x.SenderMode)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.IsNotificationsEnabled)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.MonthlyMessageQuota)
            .HasDefaultValue(1000)
            .IsRequired();

        builder.Property(x => x.MessagesUsedThisMonth)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.ActiveSenderConnectionId);

        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(x => x.WorkspaceId)
            .IsUnique()
            .HasDatabaseName("ux_workspace_whatsapp_configs_workspace_id");

        builder.HasOne(x => x.Workspace)
            .WithMany()
            .HasForeignKey(x => x.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ActiveSenderConnection)
            .WithMany()
            .HasForeignKey(x => x.ActiveSenderConnectionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
