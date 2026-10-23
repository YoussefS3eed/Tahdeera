using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NSubstitute;
using Tahdeera.Application.Common.Behaviors;
using Tahdeera.Domain.Common;
using ValidationException = Tahdeera.Application.Common.Exceptions.ValidationException;

namespace Tahdeera.Application.UnitTests.Common.Behaviors;

public class ValidationBehaviorTests
{
    public record SampleCommand(string Name) : IRequest<Result<string>>;

    [Fact]
    public async Task Handle_WhenNoValidatorsRegistered_CallsNextAndReturnsResponse()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<SampleCommand>>();
        var behavior = new ValidationBehavior<SampleCommand, Result<string>>(validators);
        var request = new SampleCommand("Valid Name");
        var expectedResponse = Result.Success("Processed");
        var nextDelegate = Substitute.For<RequestHandlerDelegate<Result<string>>>();
        nextDelegate.Invoke().Returns(Task.FromResult(expectedResponse));

        // Act
        var result = await behavior.Handle(request, nextDelegate, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        await nextDelegate.Received(1).Invoke();
    }

    [Fact]
    public async Task Handle_WhenAllValidatorsPass_CallsNextAndReturnsResponse()
    {
        // Arrange
        var validator = Substitute.For<IValidator<SampleCommand>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<SampleCommand>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ValidationResult()));

        var behavior = new ValidationBehavior<SampleCommand, Result<string>>(new[] { validator });
        var request = new SampleCommand("Valid Name");
        var expectedResponse = Result.Success("Processed");
        var nextDelegate = Substitute.For<RequestHandlerDelegate<Result<string>>>();
        nextDelegate.Invoke().Returns(Task.FromResult(expectedResponse));

        // Act
        var result = await behavior.Handle(request, nextDelegate, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        await nextDelegate.Received(1).Invoke();
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ThrowsValidationExceptionWithoutCallingNext()
    {
        // Arrange
        var failure = new ValidationFailure("Name", "Name is mandatory");
        var validator = Substitute.For<IValidator<SampleCommand>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<SampleCommand>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ValidationResult(new[] { failure })));

        var behavior = new ValidationBehavior<SampleCommand, Result<string>>(new[] { validator });
        var request = new SampleCommand("");
        var nextDelegate = Substitute.For<RequestHandlerDelegate<Result<string>>>();

        // Act
        var act = () => behavior.Handle(request, nextDelegate, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("Name");
        ex.Which.Errors["Name"].Should().Contain("Name is mandatory");
        await nextDelegate.DidNotReceive().Invoke();
    }
}
