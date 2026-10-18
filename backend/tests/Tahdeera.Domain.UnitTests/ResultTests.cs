using FluentAssertions;
using Tahdeera.Domain.Common;

namespace Tahdeera.Domain.UnitTests;

public class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_ShouldCreateFailureResult()
    {
        var error = Error.Conflict("CONFLICT_CODE", "Something went wrong.");
        var result = Result.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void SuccessT_ShouldContainValue()
    {
        var result = Result.Success("Test Value");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("Test Value");
    }

    [Fact]
    public void FailureT_AccessingValue_ShouldThrowInvalidOperationException()
    {
        var error = Error.NotFound("NOT_FOUND", "Resource missing.");
        var result = Result.Failure<string>(error);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
        var act = () => _ = result.Value;
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot access Value of a failed result.");
    }

    [Fact]
    public void ImplicitOperator_FromValue_ShouldReturnSuccess()
    {
        Result<int> result = 42;

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void ImplicitOperator_FromNullValue_ShouldReturnNullValueError()
    {
        string? nullString = null;
        Result<string> result = nullString;

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Error.NullValue);
    }

    [Fact]
    public void ImplicitOperator_FromError_ShouldReturnFailure()
    {
        var error = Error.Validation("INVALID_INPUT", "Field is required.");
        Result<int> result = error;

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }
}
