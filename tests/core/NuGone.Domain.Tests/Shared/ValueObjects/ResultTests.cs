using FluentAssertions;
using NuGone.Domain.Shared.ValueObjects;
using Xunit;

namespace NuGone.Domain.Tests.SharedValueObjects;

/// <summary>
/// Tests for the Result value object
/// </summary>
public class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        // Don't test Error property as it throws on success
    }

    [Fact]
    public void Success_WithValue_ShouldCreateSuccessfulResultWithValue()
    {
        // Arrange
        var value = "test value";

        // Act
        var result = Result<string>.Success(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(value);
        // Error is null on success, but this is implementation detail
    }

    [Fact]
    public void Failure_WithError_ShouldCreateFailureResult()
    {
        // Arrange
        var error = new Error("TestError", "Test error message");

        // Act
        var result = Result.Failure(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Failure_WithError_ShouldCreateFailureResultWithValue()
    {
        // Arrange
        var error = new Error("TestError", "Test error message");

        // Act
        var result = Result<string>.Failure(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
        // Value on failure returns default, but we don't test it specifically as it's not part of the Result pattern
    }

    [Fact]
    public void Failure_WithErrorCodeAndMessage_ShouldCreateFailureResult()
    {
        // Arrange
        var errorCode = "TestError";
        var errorMessage = "Test error message";

        // Act
        var result = Result.Failure(errorCode, errorMessage);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error.Code.Should().Be(errorCode);
        result.Error.Message.Should().Be(errorMessage);
    }

    [Fact]
    public void Failure_WithErrorCodeAndMessage_ShouldCreateFailureResultWithValue()
    {
        // Arrange
        var errorCode = "TestError";
        var errorMessage = "Test error message";

        // Act
        var result = Result<string>.Failure(errorCode, errorMessage);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error.Code.Should().Be(errorCode);
        result.Error.Message.Should().Be(errorMessage);
        // Value on failure returns default, but we don't test it specifically as it's not part of the Result pattern
    }

    [Fact]
    public void ImplicitConversionFromError_ShouldCreateFailureResult()
    {
        // Arrange
        var error = new Error("TestError", "Test error message");

        // Act
        Result result = error;

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void ImplicitConversionFromValue_ShouldCreateSuccessResult()
    {
        // Arrange
        var value = "test value";

        // Act
        Result<string> result = value;

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
    }

    [Fact]
    public void OnSuccess_Generic_WithSuccessResult_ShouldExecuteAction()
    {
        // Arrange
        var value = "test value";
        var result = Result<string>.Success(value);
        string? capturedValue = null;

        // Act
        result.OnSuccess(val => capturedValue = val);

        // Assert
        capturedValue.Should().Be(value);
    }

    [Fact]
    public void OnSuccess_Generic_WithFailureResult_ShouldNotExecuteAction()
    {
        // Arrange
        var error = new Error("TestError", "Test error message");
        var result = Result<string>.Failure(error);
        string? capturedValue = null;

        // Act
        result.OnSuccess(val => capturedValue = val);

        // Assert
        capturedValue.Should().BeNull();
    }

    [Fact]
    public void Map_WithSuccessResult_ShouldTransformValue()
    {
        // Arrange
        var value = "test value";
        var result = Result<string>.Success(value);

        // Act
        var mappedResult = result.Map(val => val.ToUpperInvariant());

        // Assert
        mappedResult.IsSuccess.Should().BeTrue();
        mappedResult.Value.Should().Be("TEST VALUE");
    }

    [Fact]
    public void Map_WithFailureResult_ShouldPropagateError()
    {
        // Arrange
        var error = new Error("TestError", "Test error message");
        var result = Result<string>.Failure(error);

        // Act
        var mappedResult = result.Map(val => val.ToUpperInvariant());

        // Assert
        mappedResult.IsFailure.Should().BeTrue();
        mappedResult.Error.Should().Be(error);
    }

    [Fact]
    public void Bind_WithSuccessResult_ShouldExecuteBinding()
    {
        // Arrange
        var value = "test value";
        var result = Result<string>.Success(value);

        // Act
        var boundResult = result.Bind(val => Result<int>.Success(val.Length));

        // Assert
        boundResult.IsSuccess.Should().BeTrue();
        boundResult.Value.Should().Be(value.Length);
    }

    [Fact]
    public void Bind_WithFailureResult_ShouldPropagateError()
    {
        // Arrange
        var error = new Error("TestError", "Test error message");
        var result = Result<string>.Failure(error);

        // Act
        var boundResult = result.Bind(val => Result<int>.Success(val.Length));

        // Assert
        boundResult.IsFailure.Should().BeTrue();
        boundResult.Error.Should().Be(error);
    }

    [Fact]
    public void GetValueOrDefault_WithSuccessResult_ShouldReturnValue()
    {
        // Arrange
        var value = "test value";
        var result = Result<string>.Success(value);
        var defaultValue = "default value";

        // Act
        var returnedValue = result.GetValueOrDefault(defaultValue);

        // Assert
        returnedValue.Should().Be(value);
    }

    [Fact]
    public void GetValueOrDefault_WithFailureResult_ShouldReturnDefaultValue()
    {
        // Arrange
        var error = new Error("TestError", "Test error message");
        var result = Result<string>.Failure(error);
        var defaultValue = "default value";

        // Act
        var returnedValue = result.GetValueOrDefault(defaultValue);

        // Assert
        returnedValue.Should().Be(defaultValue);
    }

    [Fact]
    public void ToString_WithSuccessResult_ShouldReturnSuccessMessage()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var resultString = result.ToString();

        // Assert
        resultString.Should().Be("Success");
    }

    [Fact]
    public void ToString_WithFailureResult_ShouldReturnErrorMessage()
    {
        // Arrange
        var error = new Error("TestError", "Test error message");
        var result = Result.Failure(error);

        // Act
        var resultString = result.ToString();

        // Assert
        resultString.Should().Be("Failure: [TestError] Test error message");
    }

    [Fact]
    public void GenericToString_WithSuccessResult_ShouldReturnSuccessMessageWithValue()
    {
        // Arrange
        var value = "test value";
        var result = Result<string>.Success(value);

        // Act
        var resultString = result.ToString();

        // Assert
        resultString.Should().Be($"Success: {value}");
    }

    [Fact]
    public void GenericToString_WithFailureResult_ShouldReturnErrorMessage()
    {
        // Arrange
        var error = new Error("TestError", "Test error message");
        var result = Result<string>.Failure(error);

        // Act
        var resultString = result.ToString();

        // Assert
        resultString.Should().Be("Failure: [TestError] Test error message");
    }
}
