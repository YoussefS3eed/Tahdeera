using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Infrastructure.Persistence.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("students");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(s => s.StudentCode)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(s => s.VerificationPinHash)
            .HasColumnType("bytea");

        builder.Property(s => s.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(s => s.StudentCode)
            .IsUnique();
    }
}
