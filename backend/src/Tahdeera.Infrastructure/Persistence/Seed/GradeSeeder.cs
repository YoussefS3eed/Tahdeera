using Microsoft.EntityFrameworkCore;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Infrastructure.Persistence.Seed;

public static class GradeSeeder
{
    public static async Task SeedAsync(TahdeeraDbContext context)
    {
        if (await context.Set<Grade>().AnyAsync())
        {
            return;
        }

        var grades = new List<Grade>
        {
            new() { Code = "PRI_1", NameAr = "الصف الأول الابتدائي", NameEn = "Primary 1", SortOrder = 1, IsActive = true },
            new() { Code = "PRI_2", NameAr = "الصف الثاني الابتدائي", NameEn = "Primary 2", SortOrder = 2, IsActive = true },
            new() { Code = "PRI_3", NameAr = "الصف الثالث الابتدائي", NameEn = "Primary 3", SortOrder = 3, IsActive = true },
            new() { Code = "PRI_4", NameAr = "الصف الرابع الابتدائي", NameEn = "Primary 4", SortOrder = 4, IsActive = true },
            new() { Code = "PRI_5", NameAr = "الصف الخامس الابتدائي", NameEn = "Primary 5", SortOrder = 5, IsActive = true },
            new() { Code = "PRI_6", NameAr = "الصف السادس الابتدائي", NameEn = "Primary 6", SortOrder = 6, IsActive = true },
            new() { Code = "PREP_1", NameAr = "الصف الأول الإعدادي", NameEn = "Preparatory 1", SortOrder = 7, IsActive = true },
            new() { Code = "PREP_2", NameAr = "الصف الثاني الإعدادي", NameEn = "Preparatory 2", SortOrder = 8, IsActive = true },
            new() { Code = "PREP_3", NameAr = "الصف الثالث الإعدادي", NameEn = "Preparatory 3", SortOrder = 9, IsActive = true },
            new() { Code = "SEC_1", NameAr = "الصف الأول الثانوي", NameEn = "Secondary 1", SortOrder = 10, IsActive = true },
            new() { Code = "SEC_2", NameAr = "الصف الثاني الثانوي", NameEn = "Secondary 2", SortOrder = 11, IsActive = true },
            new() { Code = "SEC_3", NameAr = "الصف الثالث الثانوي", NameEn = "Secondary 3", SortOrder = 12, IsActive = true }
        };

        await context.Set<Grade>().AddRangeAsync(grades);
        await context.SaveChangesAsync();
    }
}
