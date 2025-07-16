using NuGone.Cli.Features.RemoveCommand.Commands;
using Shouldly;
using Xunit;

namespace NuGone.Cli.Tests.Commands;

/// <summary>
/// Settings validation tests for RemoveCommand class.
/// Validates RFC-0001: CLI Architecture And Command Design - RemoveCommand settings implementation.
/// </summary>
public partial class RemoveCommandTests
{
    #region Settings Tests

    [Fact]
    public void Settings_ShouldHaveCorrectDefaultValues()
    {
        // Arrange & Act
        var settings = new RemoveCommand.Settings();

        // Assert
        settings.ProjectPath.ShouldBeNull();
        settings.ExcludePackages.ShouldBeNull();
        settings.DryRun.ShouldBeFalse();
        settings.SkipConfirmation.ShouldBeFalse();
        settings.Format.ShouldBe("text");
        settings.OutputFile.ShouldBeNull();
        settings.Verbose.ShouldBeFalse();
    }

    [Fact]
    public void Settings_ShouldAcceptProjectPath()
    {
        // Arrange & Act
        var settings = new RemoveCommand.Settings { ProjectPath = "/path/to/project" };

        // Assert
        settings.ProjectPath.ShouldBe("/path/to/project");
    }

    [Fact]
    public void Settings_ShouldAcceptExcludePackages()
    {
        // Arrange & Act
        var settings = new RemoveCommand.Settings { ExcludePackages = ["Package1", "Package2"] };

        // Assert
        settings.ExcludePackages.ShouldNotBeNull();
        settings.ExcludePackages.Length.ShouldBe(2);
        settings.ExcludePackages.ShouldContain("Package1");
        settings.ExcludePackages.ShouldContain("Package2");
    }

    [Fact]
    public void Settings_ShouldAcceptDryRunFlag()
    {
        // Arrange & Act
        var settings = new RemoveCommand.Settings { DryRun = true };

        // Assert
        settings.DryRun.ShouldBeTrue();
    }

    [Fact]
    public void Settings_ShouldAcceptSkipConfirmationFlag()
    {
        // Arrange & Act
        var settings = new RemoveCommand.Settings { SkipConfirmation = true };

        // Assert
        settings.SkipConfirmation.ShouldBeTrue();
    }

    [Fact]
    public void Settings_ShouldAcceptFormatOptions()
    {
        // Arrange & Act
        var jsonSettings = new RemoveCommand.Settings { Format = "json" };
        var textSettings = new RemoveCommand.Settings { Format = "text" };

        // Assert
        jsonSettings.Format.ShouldBe("json");
        textSettings.Format.ShouldBe("text");
    }

    [Fact]
    public void Settings_ShouldAcceptVerboseFlag()
    {
        // Arrange & Act
        var settings = new RemoveCommand.Settings { Verbose = true };

        // Assert
        settings.Verbose.ShouldBeTrue();
    }

    [Fact]
    public void Settings_ShouldAcceptOutputFile()
    {
        // Arrange & Act
        var settings = new RemoveCommand.Settings { OutputFile = "output.json" };

        // Assert
        settings.OutputFile.ShouldBe("output.json");
    }

    #endregion
}
