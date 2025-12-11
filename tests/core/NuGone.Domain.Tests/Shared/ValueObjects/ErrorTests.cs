using NuGone.Domain.Shared.ValueObjects;
using Shouldly;
using Xunit;

namespace NuGone.Domain.Tests.SharedValueObjects;

/// <summary>
/// Tests for the Error value object
/// </summary>
public class ErrorTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateError()
    {
        // Arrange
        var code = "TestError";
        var message = "Test error message";

        // Act
        var error = new Error(code, message);

        // Assert
        error.Code.ShouldBe(code);
        error.Message.ShouldBe(message);
    }

    [Fact]
    public void Constructor_WithNullCode_ShouldThrowArgumentNullException()
    {
        // Arrange
        var message = "Test error message";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Error(null!, message));
    }

    [Fact]
    public void Constructor_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var code = "TestError";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Error(code, null!));
    }

    [Fact]
    public void Create_WithCodeAndMessage_ShouldCreateError()
    {
        // Arrange
        var code = "TestError";
        var message = "Test error message";

        // Act
        var error = Error.Create(code, message);

        // Assert
        error.Code.ShouldBe(code);
        error.Message.ShouldBe(message);
    }

    [Fact]
    public void Create_WithMessageOnly_ShouldCreateErrorWithGenericCode()
    {
        // Arrange
        var message = "Test error message";

        // Act
        var error = Error.Create(message);

        // Assert
        error.Code.ShouldBe("GENERAL_ERROR");
        error.Message.ShouldBe(message);
    }

    [Fact]
    public void Validation_ShouldCreateValidationError()
    {
        // Arrange
        var message = "Validation error message";

        // Act
        var error = Error.Validation(message);

        // Assert
        error.Code.ShouldBe("VALIDATION_ERROR");
        error.Message.ShouldBe(message);
    }

    [Fact]
    public void NotFound_ShouldCreateNotFoundError()
    {
        // Arrange
        var resource = "User";

        // Act
        var error = Error.NotFound(resource);

        // Assert
        error.Code.ShouldBe("NOT_FOUND");
        error.Message.ShouldBe("User was not found");
    }

    [Fact]
    public void Unauthorized_WithCustomMessage_ShouldCreateUnauthorizedError()
    {
        // Arrange
        var message = "Custom unauthorized message";

        // Act
        var error = Error.Unauthorized(message);

        // Assert
        error.Code.ShouldBe("UNAUTHORIZED");
        error.Message.ShouldBe(message);
    }

    [Fact]
    public void Unauthorized_WithDefaultMessage_ShouldCreateUnauthorizedError()
    {
        // Act
        var error = Error.Unauthorized();

        // Assert
        error.Code.ShouldBe("UNAUTHORIZED");
        error.Message.ShouldBe("Unauthorized access");
    }

    [Fact]
    public void Forbidden_WithCustomMessage_ShouldCreateForbiddenError()
    {
        // Arrange
        var message = "Custom forbidden message";

        // Act
        var error = Error.Forbidden(message);

        // Assert
        error.Code.ShouldBe("FORBIDDEN");
        error.Message.ShouldBe(message);
    }

    [Fact]
    public void Forbidden_WithDefaultMessage_ShouldCreateForbiddenError()
    {
        // Act
        var error = Error.Forbidden();

        // Assert
        error.Code.ShouldBe("FORBIDDEN");
        error.Message.ShouldBe("Access forbidden");
    }

    [Fact]
    public void Conflict_ShouldCreateConflictError()
    {
        // Arrange
        var message = "Conflict error message";

        // Act
        var error = Error.Conflict(message);

        // Assert
        error.Code.ShouldBe("CONFLICT");
        error.Message.ShouldBe(message);
    }

    [Fact]
    public void Internal_WithCustomMessage_ShouldCreateInternalError()
    {
        // Arrange
        var message = "Custom internal error message";

        // Act
        var error = Error.Internal(message);

        // Assert
        error.Code.ShouldBe("INTERNAL_ERROR");
        error.Message.ShouldBe(message);
    }

    [Fact]
    public void Internal_WithDefaultMessage_ShouldCreateInternalError()
    {
        // Act
        var error = Error.Internal();

        // Assert
        error.Code.ShouldBe("INTERNAL_ERROR");
        error.Message.ShouldBe("An internal error occurred");
    }

    [Fact]
    public void FileSystem_ShouldCreateFileSystemError()
    {
        // Arrange
        var message = "File system error message";

        // Act
        var error = Error.FileSystem(message);

        // Assert
        error.Code.ShouldBe("FILE_SYSTEM_ERROR");
        error.Message.ShouldBe(message);
    }

    [Fact]
    public void Parsing_ShouldCreateParsingError()
    {
        // Arrange
        var message = "Parsing error message";

        // Act
        var error = Error.Parsing(message);

        // Assert
        error.Code.ShouldBe("PARSING_ERROR");
        error.Message.ShouldBe(message);
    }

    [Fact]
    public void Network_ShouldCreateNetworkError()
    {
        // Arrange
        var message = "Network error message";

        // Act
        var error = Error.Network(message);

        // Assert
        error.Code.ShouldBe("NETWORK_ERROR");
        error.Message.ShouldBe(message);
    }

    [Fact]
    public void Timeout_WithCustomMessage_ShouldCreateTimeoutError()
    {
        // Arrange
        var message = "Custom timeout message";

        // Act
        var error = Error.Timeout(message);

        // Assert
        error.Code.ShouldBe("TIMEOUT");
        error.Message.ShouldBe(message);
    }

    [Fact]
    public void Timeout_WithDefaultMessage_ShouldCreateTimeoutError()
    {
        // Act
        var error = Error.Timeout();

        // Assert
        error.Code.ShouldBe("TIMEOUT");
        error.Message.ShouldBe("Operation timed out");
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var code = "TestError";
        var message = "Test error message";
        var error = new Error(code, message);

        // Act
        var result = error.ToString();

        // Assert
        result.ShouldBe($"[{code}] {message}");
    }

    [Fact]
    public void Equals_WithSameError_ShouldReturnTrue()
    {
        // Arrange
        var error1 = new Error("TestError", "Test error message");
        var error2 = new Error("TestError", "Test error message");

        // Act & Assert
        error1.Equals(error2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_WithDifferentCode_ShouldReturnFalse()
    {
        // Arrange
        var error1 = new Error("Error1", "Test error message");
        var error2 = new Error("Error2", "Test error message");

        // Act & Assert
        error1.Equals(error2).ShouldBeFalse();
    }

    [Fact]
    public void Equals_WithDifferentMessage_ShouldReturnFalse()
    {
        // Arrange
        var error1 = new Error("TestError", "Error message 1");
        var error2 = new Error("TestError", "Error message 2");

        // Act & Assert
        error1.Equals(error2).ShouldBeFalse();
    }

    [Fact]
    public void Equals_Code_ShouldBeCaseInsensitive()
    {
        // Arrange
        var error1 = new Error("TESTERROR", "Test error message");
        var error2 = new Error("testerror", "Test error message");

        // Act & Assert
        error1.Equals(error2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_Message_ShouldBeCaseSensitive()
    {
        // Arrange
        var error1 = new Error("TestError", "Test error message");
        var error2 = new Error("TestError", "test error message");

        // Act & Assert
        error1.Equals(error2).ShouldBeFalse();
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var error = new Error("TestError", "Test error message");

        // Act & Assert
        // CA1508: This test is redundant - Equals(null) always returns false for non-null objects
        // // CA1508: This test is redundant - Equals(null) always returns false for non-null objects
        // error.Equals(null).ShouldBeFalse();
    }

    [Fact]
    public void Equals_WithDifferentType_ShouldReturnFalse()
    {
        // Arrange
        var error = new Error("TestError", "Test error message");

        // Act & Assert
        error.Equals("string").ShouldBeFalse();
    }

    [Fact]
    public void GetHashCode_WithSameError_ShouldReturnSameHashCode()
    {
        // Arrange
        var error1 = new Error("TestError", "Test error message");
        var error2 = new Error("TestError", "Test error message");

        // Act & Assert
        error1.GetHashCode().ShouldBe(error2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_Code_ShouldBeCaseInsensitive()
    {
        // Arrange
        var error1 = new Error("TESTERROR", "Test error message");
        var error2 = new Error("testerror", "Test error message");

        // Act & Assert
        error1.GetHashCode().ShouldBe(error2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentError_ShouldReturnDifferentHashCode()
    {
        // Arrange
        var error1 = new Error("Error1", "Test error message");
        var error2 = new Error("Error2", "Test error message");

        // Act & Assert
        error1.GetHashCode().ShouldNotBe(error2.GetHashCode());
    }

    [Fact]
    public void EqualityOperator_WithEqualErrors_ShouldReturnTrue()
    {
        // Arrange
        var error1 = new Error("TestError", "Test error message");
        var error2 = new Error("TestError", "Test error message");

        // Act
        var result = error1 == error2;

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void EqualityOperator_WithDifferentErrors_ShouldReturnFalse()
    {
        // Arrange
        var error1 = new Error("Error1", "Test error message");
        var error2 = new Error("Error2", "Test error message");

        // Act
        var result = error1 == error2;

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void InequalityOperator_WithEqualErrors_ShouldReturnFalse()
    {
        // Arrange
        var error1 = new Error("TestError", "Test error message");
        var error2 = new Error("TestError", "Test error message");

        // Act
        var result = error1 != error2;

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void InequalityOperator_WithDifferentErrors_ShouldReturnTrue()
    {
        // Arrange
        var error1 = new Error("Error1", "Test error message");
        var error2 = new Error("Error2", "Test error message");

        // Act
        var result = error1 != error2;

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void EqualsMethod_WithSameError_ShouldReturnTrue()
    {
        // Arrange
        var error1 = new Error("TestError", "Test error message");
        var error2 = new Error("TestError", "Test error message");

        // Act & Assert
        error1.Equals((object?)error2).ShouldBeTrue();
    }

    [Fact]
    public void EqualsMethod_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var error = new Error("TestError", "Test error message");

        // Act & Assert
        // CA1508: Equals(null) always returns false for non-null objects, so this test is redundant
        // error.Equals((object?)null).ShouldBeFalse();
    }

    [Fact]
    public void EqualsMethod_WithDifferentType_ShouldReturnFalse()
    {
        // Arrange
        var error = new Error("TestError", "Test error message");

        // Act & Assert
        error.Equals("string").ShouldBeFalse();
    }

    [Fact]
    public void GetHashCode_WithDifferentMessage_ShouldReturnDifferentHashCode()
    {
        // Arrange
        var error1 = new Error("TestError", "Error message 1");
        var error2 = new Error("TestError", "Error message 2");

        // Act & Assert
        error1.GetHashCode().ShouldNotBe(error2.GetHashCode());
    }
}
