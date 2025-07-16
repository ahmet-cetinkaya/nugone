using NuGone.Cli.Features.AnalyzeCommand.Commands;
using Shouldly;
using Xunit;

namespace NuGone.Cli.Tests.Commands;

/// <summary>
/// Settings validation tests for AnalyzeCommand class.
/// Validates RFC-0001: CLI Architecture And Command Design - AnalyzeCommand settings implementation.
/// </summary>
public partial class AnalyzeCommandTests
{
    #region Settings Tests

    [Fact]
    public void Settings_ShouldHaveCorrectDefaultValues()
    {
        // Arrange & Act
        var settings = new AnalyzeCommand.Settings();

        // Assert
        settings.ProjectPath.ShouldBeNull();
        settings.DryRun.ShouldBeTrue(); // Analyze is always dry-run by nature
        settings.Format.ShouldBe("text");
        settings.OutputFile.ShouldBeNull();
        settings.ExcludePackages.ShouldBeNull();
        settings.Verbose.ShouldBeFalse();
    }

    [Fact]
    public void Settings_ShouldAcceptProjectPath()
    {
        // Arrange & Act
        var settings = new AnalyzeCommand.Settings { ProjectPath = "/path/to/project" };

        // Assert
        settings.ProjectPath.ShouldBe("/path/to/project");
    }

    [Fact]
    public void Settings_ShouldAcceptFormatOptions()
    {
        // Arrange & Act
        var jsonSettings = new AnalyzeCommand.Settings { Format = "json" };
        var textSettings = new AnalyzeCommand.Settings { Format = "text" };

        // Assert
        jsonSettings.Format.ShouldBe("json");
        textSettings.Format.ShouldBe("text");
    }

    [Fact]
    public void Settings_ShouldAcceptExcludePackages()
    {
        // Arrange & Act
        var settings = new AnalyzeCommand.Settings { ExcludePackages = ["Package1", "Package2"] };

        // Assert
        _ = settings.ExcludePackages.ShouldNotBeNull();
        settings.ExcludePackages.Length.ShouldBe(2);
        settings.ExcludePackages.ShouldContain("Package1");
        settings.ExcludePackages.ShouldContain("Package2");
    }

    [Fact]
    public void Settings_ShouldAcceptVerboseFlag()
    {
        // Arrange & Act
        var settings = new AnalyzeCommand.Settings { Verbose = true };

        // Assert
        settings.Verbose.ShouldBeTrue();
    }

    [Fact]
    public void Settings_ShouldAcceptOutputFile()
    {
        // Arrange & Act
        var settings = new AnalyzeCommand.Settings { OutputFile = "output.json" };

        // Assert
        settings.OutputFile.ShouldBe("output.json");
    }

    #endregion
}
