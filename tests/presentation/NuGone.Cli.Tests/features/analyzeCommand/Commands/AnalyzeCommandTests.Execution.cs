using NuGone.Cli.Features.AnalyzeCommand.Commands;
using NuGone.Cli.Shared.Models;
using NuGone.Cli.Shared.Utilities;
using Shouldly;
using Spectre.Console.Cli;
using Xunit;

namespace NuGone.Cli.Tests.Commands;

/// <summary>
/// Command execution tests for AnalyzeCommand class.
/// Validates RFC-0001: CLI Architecture And Command Design - AnalyzeCommand execution flow.
/// </summary>
public partial class AnalyzeCommandTests
{
    #region Command Execution Tests

    [Fact]
    public void AnalyzeCommand_ShouldInheritFromBaseCommand()
    {
        // Arrange & Act
        var command = new AnalyzeCommand();

        // Assert
        command.ShouldBeAssignableTo<BaseCommand<AnalyzeCommand.Settings>>();
    }

    [Fact]
    public void AnalyzeCommand_ShouldImplementIAsyncCommand()
    {
        // Arrange & Act
        var command = new AnalyzeCommand();

        // Assert
        command.ShouldBeAssignableTo<IAsyncCommand<AnalyzeCommand.Settings>>();
    }

    [Fact]
    public void AnalyzeCommand_ShouldValidateProjectPath()
    {
        // Arrange
        var command = new TestableAnalyzeCommand();
        var settings = new AnalyzeCommand.Settings { ProjectPath = "/non/existent/path" };

        // Act
        var result = command.TestValidateAndResolveProjectPath(settings.ProjectPath);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("INVALID_ARGUMENT");
        result.Error.Message.ShouldContain("Project path does not exist");
    }

    [Fact]
    public void AnalyzeCommand_ShouldUseCurrentDirectoryWhenProjectPathIsNull()
    {
        // Arrange
        var command = new TestableAnalyzeCommand();
        var settings = new AnalyzeCommand.Settings { ProjectPath = null };

        // Act
        var result = command.TestValidateAndResolveProjectPath(settings.ProjectPath);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Path.GetFullPath(Directory.GetCurrentDirectory()));
    }

    [Theory]
    [InlineData("json")]
    [InlineData("JSON")]
    [InlineData("Json")]
    public void AnalyzeCommand_ShouldAcceptJsonFormat(string format)
    {
        // Arrange
        var command = new TestableAnalyzeCommand();
        var settings = new AnalyzeCommand.Settings { Format = format };

        // Act
        var isJsonFormat = command.TestIsJsonFormat(settings);

        // Assert
        isJsonFormat.ShouldBeTrue();
    }

    [Theory]
    [InlineData("text")]
    [InlineData("TEXT")]
    [InlineData("Text")]
    [InlineData("")]
    [InlineData(null)]
    public void AnalyzeCommand_ShouldTreatNonJsonAsTextFormat(string? format)
    {
        // Arrange
        var command = new TestableAnalyzeCommand();
        var settings = new AnalyzeCommand.Settings { Format = format ?? string.Empty };

        // Act
        var isJsonFormat = command.TestIsJsonFormat(settings);

        // Assert
        isJsonFormat.ShouldBeFalse();
    }

    [Fact]
    public void AnalyzeCommand_ShouldDetectVerboseMode()
    {
        // Arrange
        var command = new TestableAnalyzeCommand();
        var verboseSettings = new AnalyzeCommand.Settings { Verbose = true };
        var nonVerboseSettings = new AnalyzeCommand.Settings { Verbose = false };

        // Act
        var isVerbose = command.TestIsVerboseMode(verboseSettings);
        var isNotVerbose = command.TestIsVerboseMode(nonVerboseSettings);

        // Assert
        isVerbose.ShouldBeTrue();
        isNotVerbose.ShouldBeFalse();
    }

    #endregion
}
