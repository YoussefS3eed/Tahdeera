using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Infrastructure.Persistence.Configurations;

public class AcademicYearConfiguration : IEntityTypeConfiguration<AcademicYear>
{
    public void Configure(EntityTypeBuilder<AcademicYear> builder)
    {
        builder.ToTable("academic_years", t =>
        {
            t.HasCheckConstraint("ck_academic_years_dates", "ends_on > starts_on");
        });

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(a => a.Name)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.StartsOn)
            .IsRequired();

        builder.Property(a => a.EndsOn)
            .IsRequired();

        builder.Property(a => a.IsCurrent)
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(a => a.Name)
            .IsUnique();

        builder.HasIndex(a => a.IsCurrent)
            .IsUnique()
            .HasFilter("is_current = true")
            .HasDatabaseName("ux_one_current_academic_year");
    }
}
