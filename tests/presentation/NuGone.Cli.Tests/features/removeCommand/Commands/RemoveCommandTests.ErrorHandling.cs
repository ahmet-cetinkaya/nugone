using NuGone.Cli.Features.RemoveCommand.Commands;
using NuGone.Cli.Shared.Constants;
using Shouldly;
using Xunit;

namespace NuGone.Cli.Tests.Commands;

/// <summary>
/// Error handling tests for RemoveCommand class.
/// Validates RFC-0001: CLI Architecture And Command Design - RemoveCommand error scenarios.
/// </summary>
public partial class RemoveCommandTests
{
    #region Error Handling Tests

    [Fact]
    public void RemoveCommand_ShouldReturnErrorForInvalidProjectPath()
    {
        // Arrange
        var command = new TestableRemoveCommand();
        var settings = new RemoveCommand.Settings
        {
            ProjectPath = "/invalid/path/that/does/not/exist",
        };

        // Act
        var result = TestableRemoveCommand.TestValidateAndResolveProjectPath(settings.ProjectPath);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("INVALID_ARGUMENT");
        result.Error.ExitCode.ShouldBe(ExitCodes.InvalidArgument);
    }

    #endregion
}
