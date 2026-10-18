using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Infrastructure.Persistence.Configurations;

public class StudentCredentialConfiguration : IEntityTypeConfiguration<StudentCredential>
{
    public void Configure(EntityTypeBuilder<StudentCredential> builder)
    {
        builder.ToTable("student_credentials", t =>
        {
            t.HasCheckConstraint("ck_student_credentials_type", "credential_type IN ('QR', 'NFC', 'FallbackCode')");
            t.HasCheckConstraint("ck_student_credentials_expires_at", "expires_at IS NULL OR expires_at > issued_at");
            t.HasCheckConstraint("ck_student_credentials_revoked_at", "revoked_at IS NULL OR revoked_at >= issued_at");
        });

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(c => c.CredentialType)
            .HasConversion(
                v => v == CredentialType.Nfc ? "NFC" : v == CredentialType.Qr ? "QR" : "FallbackCode",
                v => v == "NFC" ? CredentialType.Nfc : v == "QR" ? CredentialType.Qr : CredentialType.FallbackCode)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.TokenHash)
            .HasColumnType("bytea")
            .IsRequired();

        builder.Property(c => c.CardUidDisplay)
            .HasMaxLength(64);

        builder.Property(c => c.Label)
            .HasMaxLength(100);

        builder.Property(c => c.IssuedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(c => c.RevocationReason)
            .HasColumnType("text");

        builder.HasIndex(c => c.TokenHash)
            .IsUnique()
            .HasDatabaseName("ux_student_credentials_token_hash");

        builder.HasIndex(c => c.StudentId)
            .HasFilter("revoked_at IS NULL")
            .HasDatabaseName("ix_student_credentials_student_active");

        builder.HasIndex(c => new { c.WorkspaceId, c.StudentId, c.CredentialType })
            .IsUnique()
            .HasFilter("revoked_at IS NULL AND credential_type = 'NFC'")
            .HasDatabaseName("ux_one_active_nfc_per_student_workspace");

        builder.HasOne(c => c.Student)
            .WithMany(s => s.Credentials)
            .HasForeignKey(c => c.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Workspace)
            .WithMany()
            .HasForeignKey(c => c.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.IssuedByUser)
            .WithMany()
            .HasForeignKey(c => c.IssuedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.RevokedByUser)
            .WithMany()
            .HasForeignKey(c => c.RevokedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
