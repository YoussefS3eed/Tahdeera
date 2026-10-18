using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using NSubstitute;
using Tahdeera.API.Middlewares;
using Tahdeera.Application.Common.Interfaces;
using Tahdeera.Application.Resources;
using Tahdeera.Application.UnitTests.Helpers;
using Tahdeera.Domain.Entities;

namespace Tahdeera.Application.UnitTests.Middlewares;

public class SubscriptionLockoutMiddlewareTests
{
    private readonly ITahdeeraDbContext _dbContext;
    private readonly IDateTimeProvider _clock;
    private readonly IStringLocalizer<DomainMessages> _localizer;
    private readonly DateTimeOffset _fixedTime = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    public SubscriptionLockoutMiddlewareTests()
    {
        _dbContext = Substitute.For<ITahdeeraDbContext>();
        _clock = Substitute.For<IDateTimeProvider>();
        _localizer = Substitute.For<IStringLocalizer<DomainMessages>>();

        _clock.UtcNow.Returns(_fixedTime);
        _localizer[Arg.Any<string>()].Returns(callInfo => new LocalizedString(callInfo.Arg<string>(), callInfo.Arg<string>(), false));
    }

    [Fact]
    public async Task InvokeAsync_WhitelistedRoute_ShouldPassThrough()
    {
        // Arrange
        var nextCalled = false;
        var middleware = new SubscriptionLockoutMiddleware(context =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/auth/login";
        context.Request.Method = "POST";

        // Act
        await middleware.InvokeAsync(context, _dbContext, _clock, _localizer);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_GetRequest_ShouldPassThroughRegardlessOfSubscription()
    {
        // Arrange
        var nextCalled = false;
        var middleware = new SubscriptionLockoutMiddleware(context =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var workspaceId = Guid.NewGuid();
        var context = new DefaultHttpContext();
        context.Request.Path = $"/api/workspaces/{workspaceId}/members";
        context.Request.Method = "GET";
        var routeData = new RouteData();
        routeData.Values.Add("workspaceId", workspaceId.ToString());
        context.Features.Set<IRoutingFeature>(new RoutingFeature { RouteData = routeData });

        // Act
        await middleware.InvokeAsync(context, _dbContext, _clock, _localizer);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_MutatingRequestOnExpiredSubscription_ShouldReturn403SubscriptionExpired()
    {
        // Arrange
        var nextCalled = false;
        var middleware = new SubscriptionLockoutMiddleware(context =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var workspaceId = Guid.NewGuid();
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = $"/api/workspaces/{workspaceId}/members";
        context.Request.Method = "POST";
        var routeData = new RouteData();
        routeData.Values.Add("workspaceId", workspaceId.ToString());
        context.Features.Set<IRoutingFeature>(new RoutingFeature { RouteData = routeData });

        // Expired subscription
        var expiredSub = new WorkspaceSubscription
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Status = "Active",
            EndsAt = _fixedTime.AddDays(-1)
        };

        var mockDbSet = AsyncQueryTestHelpers.CreateMockDbSet([expiredSub]);
        _dbContext.WorkspaceSubscriptions.Returns(mockDbSet);

        // Act
        await middleware.InvokeAsync(context, _dbContext, _clock, _localizer);

        // Assert
        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var json = await new StreamReader(context.Response.Body).ReadToEndAsync();
        json.Should().Contain("SUBSCRIPTION_EXPIRED");
        json.Should().Contain("01272785618");
        json.Should().Contain("01019482687");
    }

    [Fact]
    public async Task InvokeAsync_MutatingRequestOnSuspendedWorkspace_ShouldReturn403WorkspaceSuspended()
    {
        // Arrange
        var nextCalled = false;
        var middleware = new SubscriptionLockoutMiddleware(context =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var workspaceId = Guid.NewGuid();
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = $"/api/workspaces/{workspaceId}/members";
        context.Request.Method = "POST";
        var routeData = new RouteData();
        routeData.Values.Add("workspaceId", workspaceId.ToString());
        context.Features.Set<IRoutingFeature>(new RoutingFeature { RouteData = routeData });

        // Suspended subscription
        var suspendedSub = new WorkspaceSubscription
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Status = "Active",
            EndsAt = _fixedTime.AddDays(30),
            ManualSuspendedAt = _fixedTime.AddHours(-2),
            SuspensionReason = "Policy violation"
        };

        var mockDbSet = AsyncQueryTestHelpers.CreateMockDbSet([suspendedSub]);
        _dbContext.WorkspaceSubscriptions.Returns(mockDbSet);

        // Act
        await middleware.InvokeAsync(context, _dbContext, _clock, _localizer);

        // Assert
        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var json = await new StreamReader(context.Response.Body).ReadToEndAsync();
        json.Should().Contain("WORKSPACE_SUSPENDED");
        json.Should().Contain("Policy violation");
    }

    [Fact]
    public async Task InvokeAsync_WorkspaceIdMismatch_ShouldReturn400BadRequest()
    {
        // Arrange
        var nextCalled = false;
        var middleware = new SubscriptionLockoutMiddleware(context =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var routeWorkspaceId = Guid.NewGuid();
        var headerWorkspaceId = Guid.NewGuid();

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = $"/api/workspaces/{routeWorkspaceId}/members";
        context.Request.Method = "POST";
        context.Request.Headers["X-Workspace-Id"] = headerWorkspaceId.ToString();

        var routeData = new RouteData();
        routeData.Values.Add("workspaceId", routeWorkspaceId.ToString());
        context.Features.Set<IRoutingFeature>(new RoutingFeature { RouteData = routeData });

        // Act
        await middleware.InvokeAsync(context, _dbContext, _clock, _localizer);

        // Assert
        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var json = await new StreamReader(context.Response.Body).ReadToEndAsync();
        json.Should().Contain("WORKSPACE_ID_MISMATCH");
    }
}
