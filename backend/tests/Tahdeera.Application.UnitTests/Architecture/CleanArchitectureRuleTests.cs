using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using Tahdeera.API.Controllers;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Domain.Common;

namespace Tahdeera.Application.UnitTests.Architecture;

public class CleanArchitectureRuleTests
{
    private static readonly Assembly DomainAssembly = typeof(BaseEntity).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(ValidationBehavior<,>).Assembly;
    private static readonly Assembly ApiAssembly = typeof(ApiControllerBase).Assembly;

    [Fact]
    public void Domain_Should_Not_Depend_On_Application_Infrastructure_Or_API()
    {
        // Arrange
        var referencedAssemblies = DomainAssembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .ToList();

        // Assert
        referencedAssemblies.Should().NotContain(name => name.StartsWith("Tahdeera.Application"));
        referencedAssemblies.Should().NotContain(name => name.StartsWith("Tahdeera.Infrastructure"));
        referencedAssemblies.Should().NotContain(name => name.StartsWith("Tahdeera.API"));
        referencedAssemblies.Should().NotContain(name => name.Contains("EntityFrameworkCore"));
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure_Or_API()
    {
        // Arrange
        var referencedAssemblies = ApplicationAssembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .ToList();

        // Assert
        referencedAssemblies.Should().NotContain(name => name.StartsWith("Tahdeera.Infrastructure"));
        referencedAssemblies.Should().NotContain(name => name.StartsWith("Tahdeera.API"));
    }

    [Fact]
    public void All_Command_And_Query_Handlers_Must_Implement_IRequestHandler()
    {
        // Arrange
        var handlerTypes = ApplicationAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.Name.EndsWith("CommandHandler") || t.Name.EndsWith("QueryHandler"))
            .ToList();

        handlerTypes.Should().NotBeEmpty();

        // Assert
        foreach (var handler in handlerTypes)
        {
            var implementsIRequestHandler = handler.GetInterfaces()
                .Any(i => i.IsGenericType &&
                          (i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>) ||
                           i.GetGenericTypeDefinition() == typeof(IRequestHandler<>)));

            implementsIRequestHandler.Should().BeTrue(
                because: $"Handler '{handler.FullName}' must implement IRequestHandler<TRequest, TResponse> or IRequestHandler<TRequest>");
        }
    }

    [Fact]
    public void All_CQRS_Features_Commands_And_Queries_Must_Implement_IRequest()
    {
        // Arrange
        var requestTypes = ApplicationAssembly.GetTypes()
            .Where(t => t.IsClass || t.IsValueType)
            .Where(t => !t.IsAbstract && !t.IsNestedPrivate)
            .Where(t => t.Namespace != null && t.Namespace.Contains(".Features."))
            .Where(t => (t.Name.EndsWith("Command") || t.Name.EndsWith("Query")) &&
                        !t.Name.EndsWith("CommandHandler") && !t.Name.EndsWith("QueryHandler") &&
                        !t.Name.EndsWith("CommandValidator") && !t.Name.EndsWith("QueryValidator"))
            .ToList();

        requestTypes.Should().NotBeEmpty();

        // Assert
        foreach (var req in requestTypes)
        {
            var implementsIRequest = req.GetInterfaces()
                .Any(i => i == typeof(IBaseRequest));

            implementsIRequest.Should().BeTrue(
                because: $"Command or Query '{req.FullName}' must implement IRequest or IRequest<TResponse>");
        }
    }

    [Fact]
    public void All_API_Controllers_Must_Inherit_From_ApiControllerBase()
    {
        // Arrange
        var controllerTypes = ApiAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.Name.EndsWith("Controller"))
            .ToList();

        controllerTypes.Should().NotBeEmpty();

        // Assert
        foreach (var controller in controllerTypes)
        {
            controller.Should().BeAssignableTo<ControllerBase>(
                because: $"Controller '{controller.FullName}' must inherit from ControllerBase or ApiControllerBase");
        }
    }
}
