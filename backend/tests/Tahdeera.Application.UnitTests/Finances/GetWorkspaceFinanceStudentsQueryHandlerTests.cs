using FluentAssertions;
using NSubstitute;
using System.Reflection;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.Finances.Queries.GetWorkspaceFinanceStudents;
using Tahdeera.Application.UnitTests.Helpers;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;

namespace Tahdeera.Application.UnitTests.Finances;

public class GetWorkspaceFinanceStudentsQueryHandlerTests
{
    private readonly ITahdeeraDbContext _dbContext;
    private readonly GetWorkspaceFinanceStudentsHandler _handler;

    public GetWorkspaceFinanceStudentsQueryHandlerTests()
    {
        _dbContext = Substitute.For<ITahdeeraDbContext>();
        _handler = new GetWorkspaceFinanceStudentsHandler(_dbContext);
    }

    private static void AddCharge(StudentAccount account, StudentCharge charge)
    {
        var chargesField = typeof(StudentAccount).GetField("_charges", BindingFlags.NonPublic | BindingFlags.Instance)!;
        ((List<StudentCharge>)chargesField.GetValue(account)!).Add(charge);
    }

    private static void AddPayment(StudentAccount account, StudentPayment payment)
    {
        var paymentsField = typeof(StudentAccount).GetField("_payments", BindingFlags.NonPublic | BindingFlags.Instance)!;
        ((List<StudentPayment>)paymentsField.GetValue(account)!).Add(payment);
    }

    [Fact]
    public async Task Handle_WithZeroCharges_ReturnsSettled()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var student = new Student { Id = Guid.NewGuid(), FullName = "علي كمال", StudentCode = "ST-001" };
        var ws = new WorkspaceStudent { Id = Guid.NewGuid(), WorkspaceId = workspaceId, StudentId = student.Id, Student = student };
        var account = StudentAccount.Create(ws.Id);
        ws.StudentAccount = account;

        var wsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { ws });
        _dbContext.WorkspaceStudents.Returns(wsDbSet);

        var query = new GetWorkspaceFinanceStudentsQuery(workspaceId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.PaymentStatus.Should().Be("Settled");
        item.RequiredFee.Should().Be(0m);
        item.PaidAmount.Should().Be(0m);
        item.RemainingFee.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_WithChargesAndZeroPayments_ReturnsUnpaid()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var student = new Student { Id = Guid.NewGuid(), FullName = "أحمد محمد", StudentCode = "ST-002" };
        var ws = new WorkspaceStudent { Id = Guid.NewGuid(), WorkspaceId = workspaceId, StudentId = student.Id, Student = student };
        var account = StudentAccount.Create(ws.Id);
        ws.StudentAccount = account;
        AddCharge(account, StudentCharge.Create(account.Id, "October Tuition", 500m));

        var wsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { ws });
        _dbContext.WorkspaceStudents.Returns(wsDbSet);

        var query = new GetWorkspaceFinanceStudentsQuery(workspaceId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.PaymentStatus.Should().Be("Unpaid");
        item.RequiredFee.Should().Be(500m);
        item.PaidAmount.Should().Be(0m);
        item.RemainingFee.Should().Be(500m);
    }

    [Fact]
    public async Task Handle_WithChargesAndPartialPayment_ReturnsPartial()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var student = new Student { Id = Guid.NewGuid(), FullName = "سارة حسن", StudentCode = "ST-003" };
        var ws = new WorkspaceStudent { Id = Guid.NewGuid(), WorkspaceId = workspaceId, StudentId = student.Id, Student = student };
        var account = StudentAccount.Create(ws.Id);
        ws.StudentAccount = account;
        AddCharge(account, StudentCharge.Create(account.Id, "November Tuition", 600m));
        AddPayment(account, StudentPayment.Create(account.Id, "REC-101", 200m, PaymentMethod.Cash, Guid.NewGuid()));

        var wsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { ws });
        _dbContext.WorkspaceStudents.Returns(wsDbSet);

        var query = new GetWorkspaceFinanceStudentsQuery(workspaceId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.PaymentStatus.Should().Be("Partial");
        item.RequiredFee.Should().Be(600m);
        item.PaidAmount.Should().Be(200m);
        item.RemainingFee.Should().Be(400m);
    }

    [Fact]
    public async Task Handle_WithFullPayment_ReturnsPaid()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var student = new Student { Id = Guid.NewGuid(), FullName = "محمود عادل", StudentCode = "ST-004" };
        var ws = new WorkspaceStudent { Id = Guid.NewGuid(), WorkspaceId = workspaceId, StudentId = student.Id, Student = student };
        var account = StudentAccount.Create(ws.Id);
        ws.StudentAccount = account;
        AddCharge(account, StudentCharge.Create(account.Id, "December Tuition", 450m));
        AddPayment(account, StudentPayment.Create(account.Id, "REC-102", 450m, PaymentMethod.Card, Guid.NewGuid()));

        var wsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { ws });
        _dbContext.WorkspaceStudents.Returns(wsDbSet);

        var query = new GetWorkspaceFinanceStudentsQuery(workspaceId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.PaymentStatus.Should().Be("Paid");
        item.RequiredFee.Should().Be(450m);
        item.PaidAmount.Should().Be(450m);
        item.RemainingFee.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_WithStatusFilter_FiltersAccurately()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();

        // 1 Unpaid
        var s1 = new Student { Id = Guid.NewGuid(), FullName = "طالب غير مسدد", StudentCode = "ST-005" };
        var ws1 = new WorkspaceStudent { Id = Guid.NewGuid(), WorkspaceId = workspaceId, StudentId = s1.Id, Student = s1 };
        var acc1 = StudentAccount.Create(ws1.Id);
        ws1.StudentAccount = acc1;
        AddCharge(acc1, StudentCharge.Create(acc1.Id, "Tuition", 300m));

        // 1 Paid
        var s2 = new Student { Id = Guid.NewGuid(), FullName = "طالب مسدد", StudentCode = "ST-006" };
        var ws2 = new WorkspaceStudent { Id = Guid.NewGuid(), WorkspaceId = workspaceId, StudentId = s2.Id, Student = s2 };
        var acc2 = StudentAccount.Create(ws2.Id);
        ws2.StudentAccount = acc2;
        AddCharge(acc2, StudentCharge.Create(acc2.Id, "Tuition", 300m));
        AddPayment(acc2, StudentPayment.Create(acc2.Id, "REC-103", 300m, PaymentMethod.Cash, Guid.NewGuid()));

        var wsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { ws1, ws2 });
        _dbContext.WorkspaceStudents.Returns(wsDbSet);

        var query = new GetWorkspaceFinanceStudentsQuery(workspaceId, Status: "Unpaid");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].StudentCode.Should().Be("ST-005");
        result.Items[0].PaymentStatus.Should().Be("Unpaid");
    }

    [Fact]
    public async Task Handle_WithPagination_ReturnsCorrectWindowAndProperties()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var studentsList = new List<WorkspaceStudent>();

        for (int i = 1; i <= 5; i++)
        {
            var student = new Student { Id = Guid.NewGuid(), FullName = $"طالب {i:D2}", StudentCode = $"ST-10{i}" };
            var ws = new WorkspaceStudent { Id = Guid.NewGuid(), WorkspaceId = workspaceId, StudentId = student.Id, Student = student };
            var acc = StudentAccount.Create(ws.Id);
            ws.StudentAccount = acc;
            studentsList.Add(ws);
        }

        var wsDbSet = AsyncQueryTestHelpers.CreateMockDbSet(studentsList);
        _dbContext.WorkspaceStudents.Returns(wsDbSet);

        // PageIndex = 1, PageSize = 2 -> Items 3 & 4 (since 0-indexed)
        var query = new GetWorkspaceFinanceStudentsQuery(workspaceId, PageIndex: 1, PageSize: 2);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(5);
        result.TotalPages.Should().Be(3);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
        result.Items.Should().HaveCount(2);
        result.Items[0].StudentCode.Should().Be("ST-103");
        result.Items[1].StudentCode.Should().Be("ST-104");
    }
}
