using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Infrastructure.Persistence.Configurations;

public class GradeConfiguration : IEntityTypeConfiguration<Grade>
{
    public void Configure(EntityTypeBuilder<Grade> builder)
    {
        builder.ToTable("grades");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Id)
            .UseIdentityByDefaultColumn();

        builder.Property(g => g.Code)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(g => g.NameAr)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(g => g.NameEn)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(g => g.SortOrder)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(g => g.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(g => g.Code)
            .IsUnique();

        builder.HasIndex(g => g.NameAr)
            .IsUnique();

        builder.HasIndex(g => g.NameEn)
            .IsUnique();
    }
}
