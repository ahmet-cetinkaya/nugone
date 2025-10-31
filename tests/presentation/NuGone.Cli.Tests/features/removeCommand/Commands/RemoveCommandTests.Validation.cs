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

    [Theory]
    [InlineData("text")]
    [InlineData("json")]
    [InlineData("TEXT")]
    [InlineData("JSON")]
    public void RemoveCommand_ShouldAcceptValidFormats(string format)
    {
        // Arrange
        var settings = new RemoveCommand.Settings { Format = format };

        // Act
        var result = TestableRemoveCommand.TestValidateRemoveSettings(settings);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void RemoveCommand_ShouldAcceptEmptyFormat()
    {
        // Arrange
        var settings = new RemoveCommand.Settings { Format = "" };

        // Act
        var result = TestableRemoveCommand.TestValidateRemoveSettings(settings);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void RemoveCommand_ShouldAcceptNullFormat()
    {
        // Arrange
        var settings = new RemoveCommand.Settings { Format = null! };

        // Act
        var result = TestableRemoveCommand.TestValidateRemoveSettings(settings);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("xml")]
    [InlineData("csv")]
    [InlineData("yaml")]
    [InlineData("invalid")]
    [InlineData("txt")]
    public void RemoveCommand_ShouldRejectInvalidFormats(string format)
    {
        // Arrange
        var settings = new RemoveCommand.Settings { Format = format };

        // Act
        var result = TestableRemoveCommand.TestValidateRemoveSettings(settings);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBeGreaterThan(0);
        result
            .Errors.Any(error => error.Contains("Format must be either 'text' or 'json'"))
            .ShouldBeTrue();
    }

    [Fact]
    public void RemoveCommand_ShouldAcceptValidOutputFile()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var outputFile = Path.Combine(tempDir, "test-output.json");
        var settings = new RemoveCommand.Settings { OutputFile = outputFile };

        // Act
        var result = TestableRemoveCommand.TestValidateRemoveSettings(settings);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void RemoveCommand_ShouldAcceptNullOutputFile()
    {
        // Arrange
        var settings = new RemoveCommand.Settings { OutputFile = null };

        // Act
        var result = TestableRemoveCommand.TestValidateRemoveSettings(settings);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void RemoveCommand_ShouldRejectOutputFileWithNonExistentDirectory()
    {
        // Arrange
        var outputFile = Path.Combine("/non/existent/directory", "output.json");
        var settings = new RemoveCommand.Settings { OutputFile = outputFile };

        // Act
        var result = TestableRemoveCommand.TestValidateRemoveSettings(settings);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBeGreaterThan(0);
        result.Errors.Any(error => error.Contains("Output directory not found")).ShouldBeTrue();
    }

    [Fact]
    public void RemoveCommand_ShouldAcceptInvalidOutputFilePaths()
    {
        // The validation only checks directory existence, not character validation
        // These paths are considered valid because no directory check is performed
        var invalidPaths = new[] { "invalid<>path", "path|with|invalid|chars" };

        foreach (var outputPath in invalidPaths)
        {
            // Arrange
            var settings = new RemoveCommand.Settings { OutputFile = outputPath };

            // Act
            var result = TestableRemoveCommand.TestValidateRemoveSettings(settings);

            // Assert
            result.IsValid.ShouldBeTrue();
        }
    }

    [Fact]
    public void RemoveCommand_ShouldAcceptEmptyOutputFilePath()
    {
        // Arrange
        var settings = new RemoveCommand.Settings { OutputFile = "" };

        // Act
        var result = TestableRemoveCommand.TestValidateRemoveSettings(settings);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void RemoveCommand_ShouldAcceptWhitespaceOutputFilePath()
    {
        // Arrange
        var settings = new RemoveCommand.Settings { OutputFile = "   " };

        // Act
        var result = TestableRemoveCommand.TestValidateRemoveSettings(settings);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void RemoveCommand_ShouldAcceptSkipConfirmationWhenNotDryRun()
    {
        // Arrange
        var settings = new RemoveCommand.Settings { SkipConfirmation = true, DryRun = false };

        // Act
        var result = TestableRemoveCommand.TestValidateRemoveSettings(settings);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void RemoveCommand_ShouldRejectSkipConfirmationWhenDryRun()
    {
        // Arrange
        var settings = new RemoveCommand.Settings { SkipConfirmation = true, DryRun = true };

        // Act
        var result = TestableRemoveCommand.TestValidateRemoveSettings(settings);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBeGreaterThan(0);
        result
            .Errors.Any(error => error.Contains("Cannot skip confirmation in dry-run mode"))
            .ShouldBeTrue();
    }

    [Fact]
    public void RemoveCommand_ShouldAcceptValidSettingsCombination()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var outputFile = Path.Combine(tempDir, "test.json");
        var settings = new RemoveCommand.Settings
        {
            Format = "json",
            OutputFile = outputFile,
            Verbose = true,
            DryRun = false,
            SkipConfirmation = true,
            ExcludePackages = new[] { "test.package1", "test.package2" },
        };

        // Act
        var result = TestableRemoveCommand.TestValidateRemoveSettings(settings);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    #endregion
}
