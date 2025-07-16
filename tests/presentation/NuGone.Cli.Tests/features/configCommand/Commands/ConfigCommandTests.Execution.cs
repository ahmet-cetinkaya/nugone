using NuGone.Cli.Features.ConfigCommand.Commands;
using NuGone.Cli.Shared.Constants;
using NuGone.Cli.Shared.Utilities;
using Shouldly;
using Spectre.Console.Cli;
using Xunit;

namespace NuGone.Cli.Tests.Commands;

/// <summary>
/// Command execution tests for ConfigCommand class.
/// Validates RFC-0001: CLI Architecture And Command Design - ConfigCommand execution flow.
/// </summary>
public partial class ConfigCommandTests
{
    #region Command Execution Tests

    [Fact]
    public void ConfigCommand_ShouldInheritFromBaseCommand()
    {
        // Arrange & Act
        var command = new ConfigCommand();

        // Assert
        command.ShouldBeAssignableTo<BaseCommand<ConfigCommand.Settings>>();
    }

    [Fact]
    public void ConfigCommand_ShouldNotImplementIAsyncCommand()
    {
        // Arrange & Act
        var command = new ConfigCommand();

        // Assert
        command.ShouldNotBeAssignableTo<IAsyncCommand<ConfigCommand.Settings>>();
    }

    [Fact]
    public void ConfigCommand_ShouldReturnSuccessForValidSettings()
    {
        // Arrange
        var command = new TestableConfigCommand();
        var context = new CommandContext(
            Array.Empty<string>(),
            new FakeRemainingArguments(),
            "config",
            null
        );
        var settings = new ConfigCommand.Settings { Action = "list" };

        // Act
        var result = command.Execute(context, settings);

        // Assert
        result.ShouldBe(ExitCodes.Success);
    }

    #endregion
}
