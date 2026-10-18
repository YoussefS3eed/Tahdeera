using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Infrastructure.Persistence.Configurations;

public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.ToTable("subjects");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(s => s.Code)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(s => s.NameAr)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(s => s.NameEn)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(s => s.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(s => s.Code)
            .IsUnique();

        builder.HasIndex(s => s.NameAr)
            .IsUnique();

        builder.HasIndex(s => s.NameEn)
            .IsUnique();
    }
}
