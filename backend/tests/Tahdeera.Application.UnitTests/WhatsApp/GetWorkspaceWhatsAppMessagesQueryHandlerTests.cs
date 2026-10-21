using FluentAssertions;
using NSubstitute;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Features.WhatsApp.Queries.GetWorkspaceWhatsAppMessages;
using Tahdeera.Application.UnitTests.Helpers;
using Tahdeera.Domain.Entities;
using Tahdeera.Domain.Enums;
using Xunit;

namespace Tahdeera.Application.UnitTests.WhatsApp;

public class GetWorkspaceWhatsAppMessagesQueryHandlerTests
{
    private readonly ITahdeeraDbContext _dbContext;
    private readonly GetWorkspaceWhatsAppMessagesQueryHandler _handler;

    public GetWorkspaceWhatsAppMessagesQueryHandlerTests()
    {
        _dbContext = Substitute.For<ITahdeeraDbContext>();
        _handler = new GetWorkspaceWhatsAppMessagesQueryHandler(_dbContext);
    }

    [Fact]
    public async Task Handle_WhenNonOwnerQueriesMessages_ShouldMaskPhoneNumbers()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var message = MessageOutbox.Create(
            workspaceId, "+201012345678", "attendance_check_in_v1", "attendance_check_in_v1", "ar", "{}", "idem_1");

        var dbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { message });
        _dbContext.MessageOutboxes.Returns(dbSet);

        var query = new GetWorkspaceWhatsAppMessagesQuery(workspaceId, IsOwner: false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].RecipientPhone.Should().Be("+20****5678");
    }

    [Fact]
    public async Task Handle_WhenOwnerQueriesMessages_ShouldReturnUnmaskedPhoneNumbers()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var message = MessageOutbox.Create(
            workspaceId, "+201012345678", "attendance_check_in_v1", "attendance_check_in_v1", "ar", "{}", "idem_1");

        var dbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { message });
        _dbContext.MessageOutboxes.Returns(dbSet);

        var query = new GetWorkspaceWhatsAppMessagesQuery(workspaceId, IsOwner: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].RecipientPhone.Should().Be("+201012345678");
    }

    [Fact]
    public async Task Handle_WithStatusAndMessageTypeFilter_ShouldReturnOnlyMatchingMessages()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var msg1 = MessageOutbox.Create(
            workspaceId, "+201012345678", "attendance_check_in_v1", "attendance_check_in_v1", "ar", "{}", "idem_1");
        var msg2 = MessageOutbox.Create(
            workspaceId, "+201012345678", "absence_alert_v1", "absence_alert_v1", "ar", "{}", "idem_2");

        var dbSet = AsyncQueryTestHelpers.CreateMockDbSet(new[] { msg1, msg2 });
        _dbContext.MessageOutboxes.Returns(dbSet);

        var query = new GetWorkspaceWhatsAppMessagesQuery(
            workspaceId, IsOwner: true, Status: "Pending", MessageType: "attendance_check_in_v1");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].MessageType.Should().Be("attendance_check_in_v1");
    }

    [Fact]
    public async Task Handle_WithPagination_ShouldReturnCorrectPageAndTotalCount()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var messages = Enumerable.Range(1, 25).Select(i =>
            MessageOutbox.Create(
                workspaceId, $"+2010123456{i:D2}", "attendance_check_in_v1", "attendance_check_in_v1", "ar", "{}", $"idem_{i}")
        ).ToList();

        var dbSet = AsyncQueryTestHelpers.CreateMockDbSet(messages);
        _dbContext.MessageOutboxes.Returns(dbSet);

        var query = new GetWorkspaceWhatsAppMessagesQuery(workspaceId, IsOwner: true, Page: 2, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(10);
        result.Value.TotalCount.Should().Be(25);
        result.Value.PageNumber.Should().Be(2);
        result.Value.PageSize.Should().Be(10);
        result.Value.TotalPages.Should().Be(3);
    }
}
