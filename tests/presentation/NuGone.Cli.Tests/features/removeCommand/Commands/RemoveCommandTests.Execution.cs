using NuGone.Cli.Features.RemoveCommand.Commands;
using NuGone.Cli.Shared.Utilities;
using Shouldly;
using Xunit;

namespace NuGone.Cli.Tests.Commands;

/// <summary>
/// Command execution tests for RemoveCommand class.
/// Validates RFC-0001: CLI Architecture And Command Design - RemoveCommand execution flow.
/// </summary>
public partial class RemoveCommandTests
{
    #region Command Execution Tests

    [Fact]
    public void RemoveCommand_ShouldInheritFromBaseCommand()
    {
        // Arrange & Act
        var command = new RemoveCommand();

        // Assert
        _ = command.ShouldBeAssignableTo<BaseCommand<RemoveCommand.Settings>>();
    }

    [Fact]
    public void RemoveCommand_ShouldNotImplementIAsyncCommand()
    {
        // Arrange & Act
        var command = new RemoveCommand();

        // Assert
        command.ShouldNotBeAssignableTo<IAsyncCommand<RemoveCommand.Settings>>();
    }

    [Fact]
    public void RemoveCommand_ShouldValidateProjectPath()
    {
        // Arrange
        var command = new TestableRemoveCommand();
        var settings = new RemoveCommand.Settings { ProjectPath = "/non/existent/path" };

        // Act
        var result = command.TestValidateAndResolveProjectPath(settings.ProjectPath);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("INVALID_ARGUMENT");
        result.Error.Message.ShouldContain("Project path does not exist");
    }

    [Fact]
    public void RemoveCommand_ShouldUseCurrentDirectoryWhenProjectPathIsNull()
    {
        // Arrange
        var command = new TestableRemoveCommand();
        var settings = new RemoveCommand.Settings { ProjectPath = null };

        // Act
        var result = command.TestValidateAndResolveProjectPath(settings.ProjectPath);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Path.GetFullPath(Directory.GetCurrentDirectory()));
    }

    #endregion
}
