using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using System.Security.Claims;
using Tahdeera.Infrastructure.Services;

namespace Tahdeera.Application.UnitTests.Services;

public class CurrentUserServiceTests
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CurrentUserService _service;

    public CurrentUserServiceTests()
    {
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _service = new CurrentUserService(_httpContextAccessor);
    }

    [Fact]
    public void UserId_WhenNoHttpContext_ReturnsNull()
    {
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);
        _service.UserId.Should().BeNull();
    }

    [Fact]
    public void UserId_WhenNameIdentifierClaimExists_ReturnsParsedGuid()
    {
        var expectedGuid = Guid.NewGuid();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, expectedGuid.ToString()) };
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims)) };
        _httpContextAccessor.HttpContext.Returns(context);

        _service.UserId.Should().Be(expectedGuid);
    }

    [Fact]
    public void UserId_WhenSubClaimExists_ReturnsParsedGuid()
    {
        var expectedGuid = Guid.NewGuid();
        var claims = new[] { new Claim("sub", expectedGuid.ToString()) };
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims)) };
        _httpContextAccessor.HttpContext.Returns(context);

        _service.UserId.Should().Be(expectedGuid);
    }

    [Fact]
    public void Email_WhenClaimExists_ReturnsEmail()
    {
        var claims = new[] { new Claim(ClaimTypes.Email, "teacher@tahdeera.com") };
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims)) };
        _httpContextAccessor.HttpContext.Returns(context);

        _service.Email.Should().Be("teacher@tahdeera.com");
    }

    [Theory]
    [InlineData("true", false, false, true)]
    [InlineData("false", true, false, true)]
    [InlineData("false", false, true, true)]
    [InlineData("false", false, false, false)]
    [InlineData(null, false, false, false)]
    public void IsPlatformAdmin_EvaluatesCorrectly(string? claimValue, bool hasPlatformAdminRole, bool hasSuperAdminRole, bool expectedResult)
    {
        var claims = new List<Claim>();
        if (claimValue != null)
        {
            claims.Add(new Claim("is_platform_admin", claimValue));
        }
        if (hasPlatformAdminRole)
        {
            claims.Add(new Claim(ClaimTypes.Role, "PlatformAdmin"));
        }
        if (hasSuperAdminRole)
        {
            claims.Add(new Claim(ClaimTypes.Role, "SuperAdmin"));
        }

        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) };
        _httpContextAccessor.HttpContext.Returns(context);

        _service.IsPlatformAdmin.Should().Be(expectedResult);
    }

    [Fact]
    public void ActiveWorkspaceId_HeaderTakesPrecedenceOverClaim()
    {
        var headerWsId = Guid.NewGuid();
        var claimWsId = Guid.NewGuid();

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Workspace-Id"] = headerWsId.ToString();
        var claims = new[] { new Claim("workspace_id", claimWsId.ToString()) };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims));

        _httpContextAccessor.HttpContext.Returns(context);

        _service.ActiveWorkspaceId.Should().Be(headerWsId);
    }

    [Fact]
    public void ActiveWorkspaceId_WhenNoHeader_FallbackToClaim()
    {
        var claimWsId = Guid.NewGuid();
        var context = new DefaultHttpContext();
        var claims = new[] { new Claim("workspace_id", claimWsId.ToString()) };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims));

        _httpContextAccessor.HttpContext.Returns(context);

        _service.ActiveWorkspaceId.Should().Be(claimWsId);
    }
}
