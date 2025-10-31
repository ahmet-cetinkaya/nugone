using NuGone.Cli.Features.RemoveCommand.Commands;
using Shouldly;
using Xunit;

namespace NuGone.Cli.Tests.Commands;

/// <summary>
/// Confirmation logic tests for RemoveCommand class.
/// Validates RFC-0001: CLI Architecture And Command Design - RemoveCommand confirmation behavior.
/// </summary>
public partial class RemoveCommandTests
{
    #region Confirmation Logic Tests

    [Fact]
    public void RemoveCommand_ShouldSkipConfirmationWhenDryRun()
    {
        // Arrange
        var command = new TestableRemoveCommand();
        var settings = new RemoveCommand.Settings { DryRun = true };

        // Act
        var needsConfirmation = TestableRemoveCommand.TestNeedsConfirmation(settings);

        // Assert
        needsConfirmation.ShouldBeFalse();
    }

    [Fact]
    public void RemoveCommand_ShouldSkipConfirmationWhenSkipConfirmationIsTrue()
    {
        // Arrange
        var command = new TestableRemoveCommand();
        var settings = new RemoveCommand.Settings { SkipConfirmation = true };

        // Act
        var needsConfirmation = TestableRemoveCommand.TestNeedsConfirmation(settings);

        // Assert
        needsConfirmation.ShouldBeFalse();
    }

    [Fact]
    public void RemoveCommand_ShouldRequireConfirmationByDefault()
    {
        // Arrange
        var command = new TestableRemoveCommand();
        var settings = new RemoveCommand.Settings { DryRun = false, SkipConfirmation = false };

        // Act
        var needsConfirmation = TestableRemoveCommand.TestNeedsConfirmation(settings);

        // Assert
        needsConfirmation.ShouldBeTrue();
    }

    #endregion
}
