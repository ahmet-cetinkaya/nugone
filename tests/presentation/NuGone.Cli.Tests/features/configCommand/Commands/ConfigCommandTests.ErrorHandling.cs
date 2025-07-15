using NuGone.Cli.Features.ConfigCommand.Commands;
using NuGone.Cli.Shared.Constants;
using Shouldly;
using Xunit;

namespace NuGone.Cli.Tests.Commands;

/// <summary>
/// Error handling tests for ConfigCommand class.
/// Validates RFC-0001: CLI Architecture And Command Design - ConfigCommand error scenarios.
/// </summary>
public partial class ConfigCommandTests
{
    #region Error Handling Tests

    [Fact]
    public void ConfigCommand_ShouldReturnValidationErrorForInvalidAction()
    {
        // Arrange
        var command = new TestableConfigCommand();
        var settings = new ConfigCommand.Settings { Action = "invalid-action" };

        // Act
        var result = command.TestValidateConfigSettings(settings);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("VALIDATION_FAILED");
        result.Error.ExitCode.ShouldBe(ExitCodes.InvalidArgument);
    }

    #endregion
}
