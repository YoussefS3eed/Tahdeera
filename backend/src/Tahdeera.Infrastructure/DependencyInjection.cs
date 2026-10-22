using Tahdeera.Infrastructure.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Domain.Repositories;
using Tahdeera.Infrastructure.BackgroundJobs;
using Tahdeera.Infrastructure.Persistence;
using Tahdeera.Infrastructure.Persistence.Repositories;
using Tahdeera.Infrastructure.Services;

namespace Tahdeera.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        => services.AddInfrastructureServices(configuration);

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5433;Database=tahdeera;Username=postgres;Password=SetSecurePassword";

        services.AddDbContext<TahdeeraDbContext>(options =>
        {
            options.UseNpgsql(connectionString)
                   .UseSnakeCaseNamingConvention()
                   .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<ITahdeeraDbContext>(provider => provider.GetRequiredService<TahdeeraDbContext>());

        // Identity Configuration
        services.AddIdentityCore<IdentityUser<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;
            options.User.RequireUniqueEmail = true;
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<TahdeeraDbContext>()
        .AddDefaultTokenProviders();

        // JWT Authentication
        var signingKey = configuration["Jwt:SigningKey"]
            ?? "SuperSecretJwtSigningKeyWith256BitsMinimumLengthForDevelopmentOnly!";
        var issuer = configuration["Jwt:Issuer"] ?? "https://api.tahdeera.app";
        var audience = configuration["Jwt:Audience"] ?? "https://tahdeera.app";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        // Core Infrastructure Services
        services.AddHttpContextAccessor();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<ICredentialSecurityService, CredentialSecurityService>();
        services.AddScoped<IAttendanceReconciliationService, AttendanceReconciliationService>();
        // Memory Cache & Data Protection
        services.AddMemoryCache();
        services.AddDataProtection();

        // WhatsApp Infrastructure & Services
        services.Configure<Tahdeera.Infrastructure.Configuration.PlatformWhatsAppSettings>(configuration.GetSection(Tahdeera.Infrastructure.Configuration.PlatformWhatsAppSettings.SectionName));
        services.Configure<Tahdeera.Infrastructure.Configuration.WhatsAppRateCardOptions>(configuration.GetSection(Tahdeera.Infrastructure.Configuration.WhatsAppRateCardOptions.SectionName));
        services.AddSingleton<Tahdeera.Application.Common.Interfaces.Services.IWhatsAppRateCardService, Tahdeera.Infrastructure.Services.WhatsAppRateCardService>();
        services.AddScoped<Tahdeera.Application.Common.Interfaces.Services.IWhatsAppSenderResolver, Tahdeera.Infrastructure.Services.WhatsAppSenderResolver>();
        services.AddHttpClient<Tahdeera.Application.Common.Interfaces.Services.IMetaWhatsAppCloudApiClient, Tahdeera.Infrastructure.Services.MetaWhatsAppCloudApiClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddHttpClient<Tahdeera.Application.Common.Interfaces.Services.IMetaOAuthClient, Tahdeera.Infrastructure.Services.MetaOAuthClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        // MassTransit & Messaging Infrastructure
        services.AddMessagingInfrastructure(configuration);

        // Background Workers
        services.Configure<AttendanceWorkerOptions>(configuration.GetSection(AttendanceWorkerOptions.SectionName));
        services.Configure<OutboxWorkerOptions>(configuration.GetSection(OutboxWorkerOptions.SectionName));
        services.AddHostedService<SessionCompletionWorker>();
        services.AddHostedService<MessageOutboxRelayWorker>();

        // Repositories & Unit of Work
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<IWorkspaceStudentRepository, WorkspaceStudentRepository>();
        services.AddScoped<IStudentCredentialRepository, StudentCredentialRepository>();
        services.AddScoped<IGradeRepository, GradeRepository>();
        services.AddScoped<IAcademicYearRepository, AcademicYearRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IAttendanceRecordRepository, AttendanceRecordRepository>();
        services.AddScoped<IStudentAccountRepository, StudentAccountRepository>();
        services.AddScoped<IStudentChargeRepository, StudentChargeRepository>();
        services.AddScoped<IStudentPaymentRepository, StudentPaymentRepository>();
        services.AddScoped<IPaymentAllocationRepository, PaymentAllocationRepository>();
        services.AddScoped<IWorkspaceReceiptSequenceRepository, WorkspaceReceiptSequenceRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
