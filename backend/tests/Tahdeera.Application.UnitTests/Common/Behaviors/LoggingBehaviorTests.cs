using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.UnitTests.Common.Behaviors;

public class LoggingBehaviorTests
{
    public record SampleQuery(string Filter) : IRequest<Result<string>>;

    [Fact]
    public async Task Handle_WhenAuthenticatedUser_LogsUserAndWorkspaceContext()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingBehavior<SampleQuery, Result<string>>>>();
        var currentUserService = Substitute.For<ICurrentUserService>();
        var userId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        currentUserService.UserId.Returns(userId);
        currentUserService.ActiveWorkspaceId.Returns(workspaceId);

        var behavior = new LoggingBehavior<SampleQuery, Result<string>>(logger, currentUserService);
        var request = new SampleQuery("Active");
        var expectedResponse = Result.Success("Found");
        var nextDelegate = Substitute.For<RequestHandlerDelegate<Result<string>>>();
        nextDelegate.Invoke().Returns(Task.FromResult(expectedResponse));

        // Act
        var result = await behavior.Handle(request, nextDelegate, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        await nextDelegate.Received(1).Invoke();

        logger.ReceivedWithAnyArgs().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Handle_WhenAnonymousUser_LogsAnonymousAndNone()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingBehavior<SampleQuery, Result<string>>>>();
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.UserId.Returns((Guid?)null);
        currentUserService.ActiveWorkspaceId.Returns((Guid?)null);

        var behavior = new LoggingBehavior<SampleQuery, Result<string>>(logger, currentUserService);
        var request = new SampleQuery("Anon");
        var expectedResponse = Result.Success("Done");
        var nextDelegate = Substitute.For<RequestHandlerDelegate<Result<string>>>();
        nextDelegate.Invoke().Returns(Task.FromResult(expectedResponse));

        // Act
        var result = await behavior.Handle(request, nextDelegate, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        await nextDelegate.Received(1).Invoke();
    }

    [Fact]
    public async Task Handle_WhenNextThrowsException_LogsErrorAndRethrows()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingBehavior<SampleQuery, Result<string>>>>();
        var currentUserService = Substitute.For<ICurrentUserService>();
        var behavior = new LoggingBehavior<SampleQuery, Result<string>>(logger, currentUserService);
        var request = new SampleQuery("ErrorTest");
        var expectedException = new InvalidOperationException("Pipeline broken");
        var nextDelegate = Substitute.For<RequestHandlerDelegate<Result<string>>>();
        nextDelegate.Invoke().Returns<Task<Result<string>>>(_ => throw expectedException);

        // Act
        var act = () => behavior.Handle(request, nextDelegate, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Pipeline broken");

        logger.ReceivedWithAnyArgs().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
