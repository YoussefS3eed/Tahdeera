using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Tahdeera.API.Contracts.Requests;
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
public class BatchBillingLifecycleIntegrationTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public BatchBillingLifecycleIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task BulkCohortBilling_LifecycleAndAutoCreditSettlement_SucceedsEndToEnd()
    {
        // 1. Setup Workspace and Owner
        var adminClient = await _factory.CreateAdminClientAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TahdeeraDbContext>();
        var plan = await db.SubscriptionPlans.FirstAsync(p => p.Code == "TEACHER_STARTER");

        var ownerEmail = TestDataGenerator.GenerateUniqueEmail("batch_owner");
        var ownerPassword = "Password123!";

        var createWsCommand = new CreateWorkspaceCommand(
            Name: TestDataGenerator.GenerateWorkspaceName("BatchCenter"),
            Kind: "Center",
            TimezoneId: "Africa/Cairo",
            ContactPhone: TestDataGenerator.GenerateEgyptianPhone(),
            CityAddress: "Cairo",
            DefaultLateGraceMinutes: 10,
            Owner: new CreateOwnerDto(
                Username: "own_" + Guid.NewGuid().ToString("N")[..6],
                Email: ownerEmail,
                Password: ownerPassword,
                DisplayName: "Batch Center Owner",
                Phone: TestDataGenerator.GenerateEgyptianPhone()),
            Subscription: new CreateInitialSubscriptionDto(
                PlanId: plan.Id,
                StartsAt: DateTimeOffset.UtcNow,
                EndsAt: DateTimeOffset.UtcNow.AddDays(365),
                AgreedPrice: plan.DefaultPrice,
                InitialPayment: new CreateInitialPaymentDto(plan.DefaultPrice, "Cash", null, null))
        );

        var wsResponse = await adminClient.PostAsJsonAsync("/api/admin/workspaces", createWsCommand, JsonSerializationHelper.Options);
        wsResponse.EnsureSuccessStatusCode();
        var wsResult = await wsResponse.Content.ReadFromJsonAsync<CreateWorkspaceResultDto>(JsonSerializationHelper.Options);
        var workspaceId = wsResult!.WorkspaceId;

        var ownerClient = await _factory.CreateAuthenticatedClientAsync(ownerEmail, ownerPassword);
        ownerClient.DefaultRequestHeaders.Add("X-Workspace-Id", workspaceId.ToString());

        // 2. Setup Academic Year, Grade, Teacher, Group
        var academicYear = new AcademicYear
        {
            Id = Guid.NewGuid(),
            Name = "2026-2027",
            StartsOn = new DateOnly(2026, 9, 1),
            EndsOn = new DateOnly(2027, 6, 30),
            IsCurrent = true
        };
        db.AcademicYears.Add(academicYear);

        var grade = await db.Grades.FirstAsync();
        var teacherUser = await db.AppUsers.FirstAsync(u => u.NormalizedEmail == ownerEmail.ToUpperInvariant());

        var group = Group.Create(
            ownerWorkspaceId: workspaceId,
            academicYearId: academicYear.Id,
            gradeId: grade.Id,
            name: "Physics Cohort A",
            responsibleTeacherUserId: teacherUser.Id);

        db.Groups.Add(group);

        // 3. Enroll 3 active students and 1 withdrawn student
        var enrolledStudents = new List<Student>();
        for (int i = 0; i < 3; i++)
        {
            var st = new Student
            {
                Id = Guid.NewGuid(),
                FullName = $"Student {i + 1}",
                StudentCode = $"STU-{Guid.NewGuid().ToString("N")[..6]}",
                IsActive = true
            };
            var wsStudent = new WorkspaceStudent
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                StudentId = st.Id,
                Status = WorkspaceStudentStatus.Active
            };
            var gs = GroupStudent.Create(group.Id, st.Id, DateTimeOffset.UtcNow);

            db.Students.Add(st);
            db.WorkspaceStudents.Add(wsStudent);
            db.GroupStudents.Add(gs);
            enrolledStudents.Add(st);
        }

        // Withdrawn student should NOT be billed
        var withdrawnStudent = new Student
        {
            Id = Guid.NewGuid(),
            FullName = "Withdrawn Student",
            StudentCode = $"STU-{Guid.NewGuid().ToString("N")[..6]}",
            IsActive = true
        };
        var wsWithdrawn = new WorkspaceStudent
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            StudentId = withdrawnStudent.Id,
            Status = WorkspaceStudentStatus.Active
        };
        var gsWithdrawn = GroupStudent.Create(group.Id, withdrawnStudent.Id, DateTimeOffset.UtcNow.AddMonths(-1));
        gsWithdrawn.Withdraw(DateTimeOffset.UtcNow, teacherUser.Id);

        db.Students.Add(withdrawnStudent);
        db.WorkspaceStudents.Add(wsWithdrawn);
        db.GroupStudents.Add(gsWithdrawn);

        await db.SaveChangesAsync();

        // 4. Run First Batch Billing Cycle
        var billingReq = new BatchCreateGroupChargesRequest(
            BillingPeriodLabel: "2026-10",
            Description: "October Tuition Fee",
            Amount: 400.00m);

        var run1Response = await ownerClient.PostAsJsonAsync(
            $"/api/workspaces/{workspaceId}/groups/{group.Id}/finance/billing-cycles",
            billingReq,
            JsonSerializationHelper.Options);

        run1Response.StatusCode.Should().Be(HttpStatusCode.OK);
        var run1Result = await run1Response.Content.ReadFromJsonAsync<BatchCreateGroupChargesResultDto>(JsonSerializationHelper.Options);

        run1Result.Should().NotBeNull();
        run1Result!.TotalActiveEnrolled.Should().Be(3);
        run1Result.ChargesCreatedCount.Should().Be(3);
        run1Result.SkippedDuplicatesCount.Should().Be(0);
        run1Result.TotalBilledAmount.Should().Be(1200.00m);

        // 5. Re-run Identical Billing Cycle -> All skipped as duplicates
        var run2Response = await ownerClient.PostAsJsonAsync(
            $"/api/workspaces/{workspaceId}/groups/{group.Id}/finance/billing-cycles",
            billingReq,
            JsonSerializationHelper.Options);

        run2Response.StatusCode.Should().Be(HttpStatusCode.OK);
        var run2Result = await run2Response.Content.ReadFromJsonAsync<BatchCreateGroupChargesResultDto>(JsonSerializationHelper.Options);

        run2Result.Should().NotBeNull();
        run2Result!.ChargesCreatedCount.Should().Be(0);
        run2Result.SkippedDuplicatesCount.Should().Be(3);
        run2Result.TotalBilledAmount.Should().Be(0.00m);

        // 6. Record Overpayment for Student 1
        var st1 = enrolledStudents[0];
        var payReq = new RecordStudentPaymentRequest(
            Amount: 1000.00m, // 400 will settle Oct, 600 unallocated credit remains
            Method: PaymentMethod.Cash,
            PaidAt: DateTimeOffset.UtcNow,
            Reference: "CASH-PREPAY",
            Notes: "Prepaying advance months",
            Allocations: null);

        var payResponse = await ownerClient.PostAsJsonAsync(
            $"/api/workspaces/{workspaceId}/students/{st1.Id}/finance/payments",
            payReq,
            JsonSerializationHelper.Options);

        payResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // 7. Run Billing Cycle for November 2026 -> Student 1 should be auto-settled from prepaid credit
        var novBillingReq = new BatchCreateGroupChargesRequest(
            BillingPeriodLabel: "2026-11",
            Description: "November Tuition Fee",
            Amount: 400.00m);

        var run3Response = await ownerClient.PostAsJsonAsync(
            $"/api/workspaces/{workspaceId}/groups/{group.Id}/finance/billing-cycles",
            novBillingReq,
            JsonSerializationHelper.Options);

        run3Response.StatusCode.Should().Be(HttpStatusCode.OK);
        var run3Result = await run3Response.Content.ReadFromJsonAsync<BatchCreateGroupChargesResultDto>(JsonSerializationHelper.Options);

        run3Result.Should().NotBeNull();
        run3Result!.ChargesCreatedCount.Should().Be(3);
        run3Result.AutoSettledFromCreditCount.Should().BeGreaterThanOrEqualTo(1);
    }
}
