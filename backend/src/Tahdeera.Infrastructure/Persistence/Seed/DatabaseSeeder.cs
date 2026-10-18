using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.ValueObjects;

namespace Tahdeera.Infrastructure.Persistence.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(TahdeeraDbContext context, UserManager<IdentityUser<Guid>> userManager)
    {
        // 1. Seed Default Subscription Plans
        if (!await context.SubscriptionPlans.AnyAsync())
        {
            var starterPlan = new SubscriptionPlan
            {
                Code = "TEACHER_STARTER",
                NameAr = "باقة المعلم الأساسية",
                NameEn = "Teacher Starter",
                DescriptionAr = "الباقة الأساسية للمدرسين المستقلين حتى 150 طالب",
                DescriptionEn = "Starter package for independent teachers up to 150 students",
                DefaultPrice = 500.00m,
                Currency = "EGP",
                DurationDays = 365,
                MaxStudents = 150,
                MaxStaff = 1,
                MaxBranches = 1,
                Features = new SubscriptionPlanFeatures(
                    StudentsAndGroups: true,
                    NfcAttendance: true,
                    PaymentsAndFees: true,
                    WhatsappNotifications: true,
                    ExamsAndQuestionBank: false,
                    HomeworkAndLibrary: false,
                    PerformanceReports: true,
                    AiAssistant: false,
                    ExcelExport: true,
                    TeamPermissions: false),
                IsActive = true
            };

            var centerPlan = new SubscriptionPlan
            {
                Code = "PROFESSIONAL_CENTER",
                NameAr = "باقة السنتر الاحترافية",
                NameEn = "Professional Center",
                DescriptionAr = "الباقة الشاملة للمراكز التعليمية والسناتر الكبرى",
                DescriptionEn = "Comprehensive package for educational centers and institutes",
                DefaultPrice = 1500.00m,
                Currency = "EGP",
                DurationDays = 365,
                MaxStudents = 1000,
                MaxStaff = 10,
                MaxBranches = 3,
                Features = new SubscriptionPlanFeatures(
                    StudentsAndGroups: true,
                    NfcAttendance: true,
                    PaymentsAndFees: true,
                    WhatsappNotifications: true,
                    ExamsAndQuestionBank: true,
                    HomeworkAndLibrary: true,
                    PerformanceReports: true,
                    AiAssistant: true,
                    ExcelExport: true,
                    TeamPermissions: true),
                IsActive = true
            };

            await context.SubscriptionPlans.AddRangeAsync(starterPlan, centerPlan);
            await context.SaveChangesAsync();
        }

        // 2. Seed Default Super Admin User
        var adminEmail = "admin@tahdeera.app";
        var identityAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (identityAdmin == null)
        {
            var adminId = Guid.NewGuid();
            identityAdmin = new IdentityUser<Guid>
            {
                Id = adminId,
                UserName = "admin",
                Email = adminEmail,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(identityAdmin, "Admin123!456");
            if (createResult.Succeeded)
            {
                var appUser = new AppUser
                {
                    IdentitySubject = identityAdmin.Id.ToString(),
                    Username = "admin",
                    DisplayName = "Super Admin",
                    NormalizedEmail = adminEmail.ToUpperInvariant(),
                    Phone = "01000000000",
                    PreferredLanguage = "ar",
                    PreferredTheme = "light",
                    IsPlatformAdmin = true,
                    IsActive = true
                };

                await context.AppUsers.AddAsync(appUser);
                await context.SaveChangesAsync();
            }
        }
    }
}
