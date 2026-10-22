using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Infrastructure.Persistence.Configurations;

public class StudentAccountConfiguration : IEntityTypeConfiguration<StudentAccount>
{
    public void Configure(EntityTypeBuilder<StudentAccount> builder)
    {
        builder.ToTable("student_accounts");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Currency)
            .HasMaxLength(3)
            .HasDefaultValue("EGP")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        // Foreign keys & Indexes
        builder.HasIndex(x => x.WorkspaceStudentId)
            .IsUnique();

        builder.HasOne(x => x.WorkspaceStudent)
            .WithOne(ws => ws.StudentAccount)
            .HasForeignKey<StudentAccount>(x => x.WorkspaceStudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Charges)
            .WithOne(c => c.StudentAccount)
            .HasForeignKey(c => c.StudentAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Payments)
            .WithOne(p => p.StudentAccount)
            .HasForeignKey(p => p.StudentAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
