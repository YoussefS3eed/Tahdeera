using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Tahdeera.Application.Features.Finances.DTOs;
using Tahdeera.Application.Features.Workspaces.Commands.CreateWorkspace;
using Tahdeera.Application.Features.Workspaces.DTOs;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Tahdeera.Infrastructure.Persistence;
using Tahdeera.IntegrationTests.Fixtures;
using Tahdeera.IntegrationTests.Helpers;

namespace Tahdeera.IntegrationTests.Finances;

[Collection("IntegrationTests")]
public class PublicReceiptVerificationIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public PublicReceiptVerificationIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(Guid WorkspaceId, string ReceiptNumber, string ValidHash)> SetupPaymentForVerificationAsync()
    {
        var adminClient = await _factory.CreateAdminClientAsync();

        Guid planId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
            var plan = await db.SubscriptionPlans.FirstAsync(p => p.Code == "PROFESSIONAL_CENTER");
            planId = plan.Id;
        }

        var ownerEmail = TestDataGenerator.GenerateUniqueEmail("verify_owner");
        var ownerPassword = "Password123!";

        var createWsCommand = new CreateWorkspaceCommand(
            Name: "أكاديمية النجاح النموذجية",
            Kind: "Center",
            TimezoneId: "Africa/Cairo",
            ContactPhone: TestDataGenerator.GenerateEgyptianPhone(),
            CityAddress: "Cairo",
            DefaultLateGraceMinutes: 10,
            Owner: new CreateOwnerDto(
                Username: "own_" + Guid.NewGuid().ToString("N")[..6],
                Email: ownerEmail,
                Password: ownerPassword,
                DisplayName: "Verify Owner",
                Phone: TestDataGenerator.GenerateEgyptianPhone()),
            Subscription: new CreateInitialSubscriptionDto(
                PlanId: planId,
                StartsAt: DateTimeOffset.UtcNow,
                EndsAt: DateTimeOffset.UtcNow.AddDays(365),
                AgreedPrice: 500m,
                InitialPayment: new CreateInitialPaymentDto(500m, "Cash", null, null))
        );

        var wsResponse = await adminClient.PostAsJsonAsync("/api/admin/workspaces", createWsCommand, JsonSerializationHelper.Options);
        wsResponse.EnsureSuccessStatusCode();
        var wsResult = await wsResponse.Content.ReadFromJsonAsync<CreateWorkspaceResultDto>(JsonSerializationHelper.Options);

        var receiptNumber = "REC-PUB-001";
        var paidAt = new DateTimeOffset(2026, 10, 22, 14, 30, 0, TimeSpan.Zero);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();

            var student = new Student
            {
                Id = Guid.NewGuid(),
                StudentCode = "VER-001",
                FullName = "أحمد محمد كمال",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.Students.Add(student);

            var wsStudent = new WorkspaceStudent
            {
                Id = Guid.NewGuid(),
                WorkspaceId = wsResult!.WorkspaceId,
                StudentId = student.Id,
                Status = WorkspaceStudentStatus.Active,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.WorkspaceStudents.Add(wsStudent);

            var account = StudentAccount.Create(wsStudent.Id, wsResult.OwnerUserId);
            db.StudentAccounts.Add(account);

            var payment = StudentPayment.Create(
                account.Id,
                receiptNumber,
                500.00m,
                PaymentMethod.Cash,
                wsResult.OwnerUserId,
                paidAt: paidAt);
            db.StudentPayments.Add(payment);

            await db.SaveChangesAsync();
        }

        var payloadToSign = FormattableString.Invariant($"{receiptNumber}:{500.00m:F2}:{paidAt:O}:{wsResult!.WorkspaceId}:{wsResult.OwnerUserId}");
        var keyBytes = Encoding.UTF8.GetBytes(wsResult.WorkspaceId.ToString());
        var hashBytes = HMACSHA256.HashData(keyBytes, Encoding.UTF8.GetBytes(payloadToSign));
        var validHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return (wsResult.WorkspaceId, receiptNumber, validHash);
    }

    [Fact]
    public async Task VerifyReceipt_PubliclyWithoutAuth_ReturnsMaskedReceiptData()
    {
        // Arrange
        var (workspaceId, receiptNumber, validHash) = await SetupPaymentForVerificationAsync();
        var anonClient = _factory.CreateClient();

        // Act 1: Call public verification endpoint with valid hash
        var response = await anonClient.GetAsync($"/api/v1/finance/verify-receipt/{receiptNumber}/{validHash}");
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: $"Response body: {body}");
        var result = await response.Content.ReadFromJsonAsync<VerifiedReceiptDto>(JsonSerializationHelper.Options);
        result.Should().NotBeNull();
        result!.IsValid.Should().BeTrue();
        result.ReceiptNumber.Should().Be(receiptNumber);
        result.StudentNameMasked.Should().Be("أحمد م***");
        result.WorkspaceName.Should().Be("أكاديمية النجاح النموذجية");
        result.AmountEgp.Should().Be(500.00m);
        result.Status.Should().Be("Received");
        result.ReversedAtUtc.Should().BeNull();

        // Act 2: Altered hash should return 404 NotFound
        var fakeHash = new string('f', 64);
        var badResponse = await anonClient.GetAsync($"/api/v1/finance/verify-receipt/{receiptNumber}/{fakeHash}");
        badResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
