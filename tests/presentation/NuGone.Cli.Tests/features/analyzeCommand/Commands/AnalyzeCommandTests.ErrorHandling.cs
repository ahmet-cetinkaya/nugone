using NuGone.Cli.Features.AnalyzeCommand.Commands;
using NuGone.Cli.Shared.Constants;
using Shouldly;
using Xunit;

namespace NuGone.Cli.Tests.Commands;

/// <summary>
/// Error handling tests for AnalyzeCommand class.
/// Validates RFC-0001: CLI Architecture And Command Design - AnalyzeCommand error scenarios.
/// </summary>
public partial class AnalyzeCommandTests
{
    #region Error Handling Tests

    [Fact]
    public void AnalyzeCommand_ShouldReturnErrorForInvalidProjectPath()
    {
        // Arrange
        var command = new TestableAnalyzeCommand();
        var settings = new AnalyzeCommand.Settings
        {
            ProjectPath = "/invalid/path/that/does/not/exist",
        };

        // Act
        var result = command.TestValidateAndResolveProjectPath(settings.ProjectPath);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("INVALID_ARGUMENT");
        result.Error.ExitCode.ShouldBe(ExitCodes.InvalidArgument);
    }

    #endregion
}
