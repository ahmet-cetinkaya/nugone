using NuGone.Cli.Features.ConfigCommand.Commands;
using Shouldly;
using Xunit;

namespace NuGone.Cli.Tests.Commands;

/// <summary>
/// Validation tests for ConfigCommand class.
/// Validates RFC-0001: CLI Architecture And Command Design - ConfigCommand input validation and business rules.
/// </summary>
public partial class ConfigCommandTests
{
    #region Validation Tests

    [Theory]
    [InlineData("get")]
    [InlineData("list")]
    [InlineData("reset")]
    [InlineData("GET")]
    [InlineData("LIST")]
    [InlineData("RESET")]
    public void ConfigCommand_ShouldAcceptValidActions(string action)
    {
        // Arrange
        var command = new TestableConfigCommand();
        var settings = new ConfigCommand.Settings { Action = action };

        // Act
        var result = command.TestValidateConfigSettings(settings);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("set")]
    [InlineData("SET")]
    public void ConfigCommand_ShouldAcceptSetActionWithKeyAndValue(string action)
    {
        // Arrange
        var command = new TestableConfigCommand();
        var settings = new ConfigCommand.Settings
        {
            Action = action,
            Key = "testKey",
            Value = "testValue",
        };

        // Act
        var result = command.TestValidateConfigSettings(settings);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("delete")]
    [InlineData("create")]
    [InlineData("unknown")]
    public void ConfigCommand_ShouldRejectInvalidActions(string action)
    {
        // Arrange
        var command = new TestableConfigCommand();
        var settings = new ConfigCommand.Settings { Action = action };

        // Act
        var result = command.TestValidateConfigSettings(settings);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("VALIDATION_FAILED");
        result.Error.Message.ShouldContain("Action must be one of: get, set, list, reset");
        result.Error.Details.ShouldContainKey("ProvidedAction");
        result.Error.Details["ProvidedAction"].ShouldBe(action);
    }

    [Fact]
    public void ConfigCommand_ShouldAcceptNullAction()
    {
        // Arrange
        var command = new TestableConfigCommand();
        var settings = new ConfigCommand.Settings { Action = null };

        // Act
        var result = command.TestValidateConfigSettings(settings);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ConfigCommand_ShouldAcceptEmptyAction()
    {
        // Arrange
        var command = new TestableConfigCommand();
        var settings = new ConfigCommand.Settings { Action = string.Empty };

        // Act
        var result = command.TestValidateConfigSettings(settings);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ConfigCommand_ShouldRequireKeyAndValueForSetAction()
    {
        // Arrange
        var command = new TestableConfigCommand();
        var settings = new ConfigCommand.Settings
        {
            Action = "set",
            Key = "testKey",
            Value = "testValue",
        };

        // Act
        var result = command.TestValidateConfigSettings(settings);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ConfigCommand_ShouldFailWhenSetActionMissingKey()
    {
        // Arrange
        var command = new TestableConfigCommand();
        var settings = new ConfigCommand.Settings
        {
            Action = "set",
            Key = null,
            Value = "testValue",
        };

        // Act
        var result = command.TestValidateConfigSettings(settings);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("VALIDATION_FAILED");
        result.Error.Message.ShouldContain("Both key and value are required for 'set' action");
    }

    [Fact]
    public void ConfigCommand_ShouldFailWhenSetActionMissingValue()
    {
        // Arrange
        var command = new TestableConfigCommand();
        var settings = new ConfigCommand.Settings
        {
            Action = "set",
            Key = "testKey",
            Value = null,
        };

        // Act
        var result = command.TestValidateConfigSettings(settings);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("VALIDATION_FAILED");
        result.Error.Message.ShouldContain("Both key and value are required for 'set' action");
    }

    [Fact]
    public void ConfigCommand_ShouldFailWhenSetActionMissingBothKeyAndValue()
    {
        // Arrange
        var command = new TestableConfigCommand();
        var settings = new ConfigCommand.Settings
        {
            Action = "set",
            Key = null,
            Value = null,
        };

        // Act
        var result = command.TestValidateConfigSettings(settings);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("VALIDATION_FAILED");
        result.Error.Message.ShouldContain("Both key and value are required for 'set' action");
    }

    [Fact]
    public void ConfigCommand_ShouldFailWhenSetActionHasEmptyKey()
    {
        // Arrange
        var command = new TestableConfigCommand();
        var settings = new ConfigCommand.Settings
        {
            Action = "set",
            Key = string.Empty,
            Value = "testValue",
        };

        // Act
        var result = command.TestValidateConfigSettings(settings);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("VALIDATION_FAILED");
        result.Error.Message.ShouldContain("Both key and value are required for 'set' action");
    }

    [Fact]
    public void ConfigCommand_ShouldFailWhenSetActionHasEmptyValue()
    {
        // Arrange
        var command = new TestableConfigCommand();
        var settings = new ConfigCommand.Settings
        {
            Action = "set",
            Key = "testKey",
            Value = string.Empty,
        };

        // Act
        var result = command.TestValidateConfigSettings(settings);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("VALIDATION_FAILED");
        result.Error.Message.ShouldContain("Both key and value are required for 'set' action");
    }

    [Theory]
    [InlineData("get")]
    [InlineData("list")]
    [InlineData("reset")]
    public void ConfigCommand_ShouldNotRequireKeyAndValueForNonSetActions(string action)
    {
        // Arrange
        var command = new TestableConfigCommand();
        var settings = new ConfigCommand.Settings
        {
            Action = action,
            Key = null,
            Value = null,
        };

        // Act
        var result = command.TestValidateConfigSettings(settings);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    #endregion
}
