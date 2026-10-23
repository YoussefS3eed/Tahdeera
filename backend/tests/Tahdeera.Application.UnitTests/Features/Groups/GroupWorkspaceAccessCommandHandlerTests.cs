using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tahdeera.Application.Features.Groups.Commands.GroupWorkspaceAccess;
using Tahdeera.Application.UnitTests.Common;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Application.UnitTests.Features.Groups;

public class GroupWorkspaceAccessCommandHandlerTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _granteeWorkspaceId = Guid.NewGuid();
    private readonly Guid _academicYearId = Guid.NewGuid();
    private readonly Guid _actorUserId = Guid.NewGuid();

    [Fact]
    public async Task HandleGrant_WhenValid_ShouldCreateAccess()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var group = Group.Create(_workspaceId, _academicYearId, 10, "Math A", _actorUserId);
        context.Groups.Add(group);

        var granteeWorkspace = new Workspace
        {
            Id = _granteeWorkspaceId,
            Name = "Partner Center",
            Kind = "Center",
            CreatedByUserId = _actorUserId
        };
        context.Workspaces.Add(granteeWorkspace);
        await context.SaveChangesAsync();

        var handler = new GrantGroupWorkspaceAccessCommandHandler(new Tahdeera.Infrastructure.Persistence.Repositories.UnitOfWork(context));
        var command = new GrantGroupWorkspaceAccessCommand(
            WorkspaceId: _workspaceId,
            GroupId: group.Id,
            GranteeWorkspaceId: _granteeWorkspaceId,
            CanViewRoster: true,
            CanRegisterAttendance: true,
            CanEditAttendance: false,
            ExpiresAt: null,
            ActorUserId: _actorUserId,
            Notes: "For attendance recording at front desk");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CanViewRoster.Should().BeTrue();
        result.Value.CanRegisterAttendance.Should().BeTrue();
        result.Value.CanEditAttendance.Should().BeFalse();
        result.Value.GranteeWorkspaceName.Should().Be("Partner Center");

        var accessInDb = await context.GroupWorkspaceAccesses.FirstAsync(a => a.Id == result.Value.Id);
        accessInDb.CanViewRoster.Should().BeTrue();
        accessInDb.RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task HandleRevoke_ShouldMarkAccessRevoked()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var group = Group.Create(_workspaceId, _academicYearId, 10, "Math A", _actorUserId);
        context.Groups.Add(group);

        var access = GroupWorkspaceAccess.Grant(group.Id, _granteeWorkspaceId, _actorUserId, true, true, false);
        context.GroupWorkspaceAccesses.Add(access);
        await context.SaveChangesAsync();

        var handler = new RevokeGroupWorkspaceAccessCommandHandler(new Tahdeera.Infrastructure.Persistence.Repositories.UnitOfWork(context));
        var command = new RevokeGroupWorkspaceAccessCommand(_workspaceId, access.Id, _actorUserId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var accessInDb = await context.GroupWorkspaceAccesses.FirstAsync(a => a.Id == access.Id);
        accessInDb.RevokedAt.Should().NotBeNull();
    }
}
