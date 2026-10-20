using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tahdeera.Application.Features.Groups.Commands.AssignAttendanceDelegate;
using Tahdeera.Application.Features.Groups.Commands.RevokeAttendanceDelegate;
using Tahdeera.Application.UnitTests.Common;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Application.UnitTests.Features.Groups;

public class GroupDelegationCommandHandlerTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _academicYearId = Guid.NewGuid();
    private readonly Guid _teacherUserId = Guid.NewGuid();
    private readonly Guid _assistant1UserId = Guid.NewGuid();
    private readonly Guid _assistant2UserId = Guid.NewGuid();

    [Fact]
    public async Task HandleAssign_WhenFirstDelegate_ShouldCreateActiveDelegation()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var group = Group.Create(_workspaceId, _academicYearId, 10, "Physics A", _teacherUserId);
        context.Groups.Add(group);
        context.AppUsers.Add(new AppUser { Id = _assistant1UserId, DisplayName = "Assistant One" });
        await context.SaveChangesAsync();

        var handler = new AssignAttendanceDelegateCommandHandler(new Tahdeera.Infrastructure.Persistence.Repositories.UnitOfWork(context));
        var command = new AssignAttendanceDelegateCommand(_workspaceId, group.Id, _assistant1UserId, _teacherUserId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AssistantUserId.Should().Be(_assistant1UserId);
        result.Value.RevokedAt.Should().BeNull();

        var delegationInDb = await context.GroupAttendanceDelegations.FirstAsync(d => d.Id == result.Value.Id);
        delegationInDb.RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task HandleAssign_WhenSecondDelegateAssigned_ShouldAutoRevokeFirstDelegate()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var group = Group.Create(_workspaceId, _academicYearId, 10, "Physics A", _teacherUserId);
        context.Groups.Add(group);
        context.AppUsers.Add(new AppUser { Id = _assistant1UserId, DisplayName = "Assistant One" });
        context.AppUsers.Add(new AppUser { Id = _assistant2UserId, DisplayName = "Assistant Two" });

        var firstDelegation = GroupAttendanceDelegation.Create(group.Id, _assistant1UserId, _teacherUserId);
        context.GroupAttendanceDelegations.Add(firstDelegation);
        await context.SaveChangesAsync();

        var handler = new AssignAttendanceDelegateCommandHandler(new Tahdeera.Infrastructure.Persistence.Repositories.UnitOfWork(context));
        var command = new AssignAttendanceDelegateCommand(_workspaceId, group.Id, _assistant2UserId, _teacherUserId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AssistantUserId.Should().Be(_assistant2UserId);

        var firstInDb = await context.GroupAttendanceDelegations.FirstAsync(d => d.Id == firstDelegation.Id);
        firstInDb.RevokedAt.Should().NotBeNull();
        firstInDb.RevocationReason.Should().Be("Replaced by new delegate");

        var secondInDb = await context.GroupAttendanceDelegations.FirstAsync(d => d.Id == result.Value.Id);
        secondInDb.RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task HandleRevoke_ShouldMarkDelegationRevoked()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var group = Group.Create(_workspaceId, _academicYearId, 10, "Physics A", _teacherUserId);
        context.Groups.Add(group);

        var delegation = GroupAttendanceDelegation.Create(group.Id, _assistant1UserId, _teacherUserId);
        context.GroupAttendanceDelegations.Add(delegation);
        await context.SaveChangesAsync();

        var handler = new RevokeAttendanceDelegateCommandHandler(new Tahdeera.Infrastructure.Persistence.Repositories.UnitOfWork(context));
        var command = new RevokeAttendanceDelegateCommand(_workspaceId, group.Id, delegation.Id, "Terminated assignment", _teacherUserId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var delegationInDb = await context.GroupAttendanceDelegations.FirstAsync(d => d.Id == delegation.Id);
        delegationInDb.RevokedAt.Should().NotBeNull();
        delegationInDb.RevocationReason.Should().Be("Terminated assignment");
    }
}
