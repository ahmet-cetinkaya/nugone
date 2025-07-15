using NuGone.Cli.Features.RemoveCommand.Commands;
using Shouldly;
using Xunit;

namespace NuGone.Cli.Tests.Commands;

/// <summary>
/// Validation tests for RemoveCommand class.
/// Validates RFC-0001: CLI Architecture And Command Design - RemoveCommand input validation and business rules.
/// </summary>
public partial class RemoveCommandTests
{
    #region Validation Tests

    [Fact]
    public void RemoveCommand_ShouldValidateSettingsSuccessfully()
    {
        // Arrange
        var command = new TestableRemoveCommand();
        var settings = new RemoveCommand.Settings
        {
            ProjectPath = Directory.GetCurrentDirectory(),
            DryRun = false,
            SkipConfirmation = true,
        };

        // Act
        var result = command.TestValidateRemoveSettings(settings);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void RemoveCommand_ShouldFailValidationForCriticalPackageExclusion()
    {
        // Arrange
        var command = new TestableRemoveCommand();
        var settings = new RemoveCommand.Settings
        {
            ExcludePackages = new[] { "critical-package" },
        };

        // Act
        var result = command.TestPerformRemoval(Directory.GetCurrentDirectory(), settings);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("OPERATION_FAILED");
        result.Error.Message.ShouldContain("Cannot exclude critical system packages");
    }

    [Fact]
    public void RemoveCommand_ShouldFailForReadOnlyPath()
    {
        // Arrange
        var command = new TestableRemoveCommand();
        var settings = new RemoveCommand.Settings();
        var readOnlyPath = Path.Combine("readonly", "project");

        // Act
        var result = command.TestPerformRemoval(readOnlyPath, settings);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("ACCESS_DENIED");
    }

    [Fact]
    public void RemoveCommand_ShouldSucceedForValidSettings()
    {
        // Arrange
        var command = new TestableRemoveCommand();
        var settings = new RemoveCommand.Settings { ExcludePackages = new[] { "valid-package" } };

        // Act
        var result = command.TestPerformRemoval(Directory.GetCurrentDirectory(), settings);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    #endregion
}
