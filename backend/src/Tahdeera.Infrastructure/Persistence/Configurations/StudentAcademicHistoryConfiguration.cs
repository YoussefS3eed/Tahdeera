using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Infrastructure.Persistence.Configurations;

public class StudentAcademicHistoryConfiguration : IEntityTypeConfiguration<StudentAcademicHistory>
{
    public void Configure(EntityTypeBuilder<StudentAcademicHistory> builder)
    {
        builder.ToTable("student_academic_history");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(h => h.RecordedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(h => h.Notes)
            .HasColumnType("text");

        builder.HasIndex(h => new { h.StudentId, h.AcademicYearId })
            .IsUnique();

        builder.HasIndex(h => new { h.StudentId, h.AcademicYearId, h.GradeId })
            .HasDatabaseName("ix_student_academic_history_lookup");

        builder.HasOne(h => h.Student)
            .WithMany(s => s.AcademicHistories)
            .HasForeignKey(h => h.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.AcademicYear)
            .WithMany(a => a.AcademicHistories)
            .HasForeignKey(h => h.AcademicYearId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.Grade)
            .WithMany(g => g.AcademicHistories)
            .HasForeignKey(h => h.GradeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.RecordedByUser)
            .WithMany()
            .HasForeignKey(h => h.RecordedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
