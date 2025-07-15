using NuGone.Cli.Features.ConfigCommand.Commands;
using Shouldly;
using Xunit;

namespace NuGone.Cli.Tests.Commands;

/// <summary>
/// Settings validation tests for ConfigCommand class.
/// Validates RFC-0001: CLI Architecture And Command Design - ConfigCommand settings implementation.
/// </summary>
public partial class ConfigCommandTests
{
    #region Settings Tests

    [Fact]
    public void Settings_ShouldHaveCorrectDefaultValues()
    {
        // Arrange & Act
        var settings = new ConfigCommand.Settings();

        // Assert
        settings.Action.ShouldBeNull();
        settings.Key.ShouldBeNull();
        settings.Value.ShouldBeNull();
        settings.Global.ShouldBeFalse();
    }

    [Fact]
    public void Settings_ShouldAcceptAction()
    {
        // Arrange & Act
        var settings = new ConfigCommand.Settings { Action = "list" };

        // Assert
        settings.Action.ShouldBe("list");
    }

    [Fact]
    public void Settings_ShouldAcceptKey()
    {
        // Arrange & Act
        var settings = new ConfigCommand.Settings { Key = "excludePatterns" };

        // Assert
        settings.Key.ShouldBe("excludePatterns");
    }

    [Fact]
    public void Settings_ShouldAcceptValue()
    {
        // Arrange & Act
        var settings = new ConfigCommand.Settings { Value = "System.Text.Json" };

        // Assert
        settings.Value.ShouldBe("System.Text.Json");
    }

    [Fact]
    public void Settings_ShouldAcceptGlobalFlag()
    {
        // Arrange & Act
        var settings = new ConfigCommand.Settings { Global = true };

        // Assert
        settings.Global.ShouldBeTrue();
    }

    #endregion
}
