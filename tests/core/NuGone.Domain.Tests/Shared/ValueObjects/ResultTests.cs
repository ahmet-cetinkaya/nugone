using Shouldly;
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
        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
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
        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Value.ShouldBe(value);
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
        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(error);
    }

    [Fact]
    public void Failure_WithError_ShouldCreateFailureResultWithValue()
    {
        // Arrange
        var error = new Error("TestError", "Test error message");

        // Act
        var result = Result<string>.Failure(error);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(error);
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
        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldNotBeNull();
        result.Error.Code.ShouldBe(errorCode);
        result.Error.Message.ShouldBe(errorMessage);
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
        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldNotBeNull();
        result.Error.Code.ShouldBe(errorCode);
        result.Error.Message.ShouldBe(errorMessage);
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
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(error);
    }

    [Fact]
    public void ImplicitConversionFromValue_ShouldCreateSuccessResult()
    {
        // Arrange
        var value = "test value";

        // Act
        Result<string> result = value;

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(value);
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
        capturedValue.ShouldBe(value);
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
        capturedValue.ShouldBe(null);
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
        mappedResult.IsSuccess.ShouldBeTrue();
        mappedResult.Value.ShouldBe("TEST VALUE");
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
        mappedResult.IsFailure.ShouldBeTrue();
        mappedResult.Error.ShouldBe(error);
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
        boundResult.IsSuccess.ShouldBeTrue();
        boundResult.Value.ShouldBe(value.Length);
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
        boundResult.IsFailure.ShouldBeTrue();
        boundResult.Error.ShouldBe(error);
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
        returnedValue.ShouldBe(value);
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
        returnedValue.ShouldBe(defaultValue);
    }

    [Fact]
    public void ToString_WithSuccessResult_ShouldReturnSuccessMessage()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var resultString = result.ToString();

        // Assert
        resultString.ShouldBe("Success");
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
        resultString.ShouldBe("Failure: [TestError] Test error message");
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
        resultString.ShouldBe($"Success: {value}");
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
        resultString.ShouldBe("Failure: [TestError] Test error message");
    }
}
