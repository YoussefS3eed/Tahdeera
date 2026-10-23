using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Security.Cryptography;
using System.Text;
using Tahdeera.API.Security;
using Tahdeera.Infrastructure.Configuration;

namespace Tahdeera.Application.UnitTests.WhatsApp;

public sealed class MetaWebhookHmacAttributeTests
{
    private const string AppSecret = "test_meta_app_secret_abc123";
    private readonly MetaWebhookHmacAttribute _filter;

    public MetaWebhookHmacAttributeTests()
    {
        var settings = new PlatformWhatsAppSettings
        {
            AppSecret = AppSecret
        };
        var logger = Substitute.For<ILogger<MetaWebhookHmacAttribute>>();
        _filter = new MetaWebhookHmacAttribute(Options.Create(settings), logger);
    }

    [Fact]
    public async Task OnResourceExecutionAsync_MissingHeader_Returns401Unauthorized()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"test\":\"data\"}"));
        var context = CreateResourceExecutingContext(httpContext);
        bool nextCalled = false;

        await _filter.OnResourceExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ResourceExecutedContext(context, new List<IFilterMetadata>()));
        });

        nextCalled.Should().BeFalse();
        context.Result.Should().BeOfType<UnauthorizedObjectResult>();
        var unauthorizedResult = (UnauthorizedObjectResult)context.Result!;
        unauthorizedResult.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        var problem = unauthorizedResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["errorCode"].Should().Be("WEBHOOK_SIGNATURE_INVALID");
    }

    [Fact]
    public async Task OnResourceExecutionAsync_InvalidSignature_Returns401Unauthorized()
    {
        var httpContext = new DefaultHttpContext();
        var rawBody = "{\"test\":\"data\"}";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(rawBody));
        httpContext.Request.Headers["X-Hub-Signature-256"] = "sha256=invalid_hex_digest_99999999999999999999";
        var context = CreateResourceExecutingContext(httpContext);
        bool nextCalled = false;

        await _filter.OnResourceExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ResourceExecutedContext(context, new List<IFilterMetadata>()));
        });

        nextCalled.Should().BeFalse();
        context.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task OnResourceExecutionAsync_ValidSignature_CallsNext()
    {
        var rawBody = "{\"entry\":[{\"id\":\"123\"}]}";
        var bodyBytes = Encoding.UTF8.GetBytes(rawBody);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(AppSecret));
        var validHash = Convert.ToHexStringLower(hmac.ComputeHash(bodyBytes));

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(bodyBytes);
        httpContext.Request.Headers["X-Hub-Signature-256"] = $"sha256={validHash}";
        var context = CreateResourceExecutingContext(httpContext);
        bool nextCalled = false;

        await _filter.OnResourceExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ResourceExecutedContext(context, new List<IFilterMetadata>()));
        });

        nextCalled.Should().BeTrue();
        context.Result.Should().BeNull();
    }

    private static ResourceExecutingContext CreateResourceExecutingContext(HttpContext httpContext)
    {
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ResourceExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new List<IValueProviderFactory>());
    }
}
