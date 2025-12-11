using NuGone.Cli.Features.AnalyzeCommand.Commands;
using Shouldly;
using Xunit;

namespace NuGone.Cli.Tests.Commands;

/// <summary>
/// Validation tests for AnalyzeCommand class.
/// Validates RFC-0001: CLI Architecture And Command Design - AnalyzeCommand input validation and business rules.
/// </summary>
public partial class AnalyzeCommandTests
{
    #region Validation Tests

    [Theory]
    [InlineData("text")]
    [InlineData("json")]
    [InlineData("TEXT")]
    [InlineData("JSON")]
    public void AnalyzeCommand_ShouldAcceptValidFormats(string format)
    {
        // Arrange
        var settings = new AnalyzeCommand.Settings { Format = format };

        // Act
        var result = TestableAnalyzeCommand.TestValidateAnalyzeSettings(settings);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void AnalyzeCommand_ShouldAcceptEmptyFormat()
    {
        // Arrange
        var settings = new AnalyzeCommand.Settings { Format = "" };

        // Act
        var result = TestableAnalyzeCommand.TestValidateAnalyzeSettings(settings);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void AnalyzeCommand_ShouldAcceptNullFormat()
    {
        // Arrange
        var settings = new AnalyzeCommand.Settings { Format = null! };

        // Act
        var result = TestableAnalyzeCommand.TestValidateAnalyzeSettings(settings);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("xml")]
    [InlineData("csv")]
    [InlineData("yaml")]
    [InlineData("invalid")]
    [InlineData("txt")]
    public void AnalyzeCommand_ShouldRejectInvalidFormats(string format)
    {
        // Arrange
        var settings = new AnalyzeCommand.Settings { Format = format };

        // Act
        var result = TestableAnalyzeCommand.TestValidateAnalyzeSettings(settings);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBeGreaterThan(0);
        result
            .Errors.Any(error =>
                error.Contains("Format must be either 'text' or 'json'", StringComparison.Ordinal)
            )
            .ShouldBeTrue();
    }

    [Fact]
    public void AnalyzeCommand_ShouldAcceptValidOutputFile()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var outputFile = Path.Combine(tempDir, "test-output.json");
        var settings = new AnalyzeCommand.Settings { OutputFile = outputFile };

        // Act
        var result = TestableAnalyzeCommand.TestValidateAnalyzeSettings(settings);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void AnalyzeCommand_ShouldAcceptNullOutputFile()
    {
        // Arrange
        var settings = new AnalyzeCommand.Settings { OutputFile = null };

        // Act
        var result = TestableAnalyzeCommand.TestValidateAnalyzeSettings(settings);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void AnalyzeCommand_ShouldRejectOutputFileWithNonExistentDirectory()
    {
        // Arrange
        var outputFile = Path.Combine("/non/existent/directory", "output.json");
        var settings = new AnalyzeCommand.Settings { OutputFile = outputFile };

        // Act
        var result = TestableAnalyzeCommand.TestValidateAnalyzeSettings(settings);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBeGreaterThan(0);
        result
            .Errors.Any(error =>
                error.Contains("Output directory not found", StringComparison.Ordinal)
            )
            .ShouldBeTrue();
    }

    [Fact]
    public void AnalyzeCommand_ShouldAcceptInvalidOutputFilePaths()
    {
        // The validation only checks directory existence, not character validation
        // These paths are considered valid because no directory check is performed
        var invalidPaths = new[] { "invalid<>path", "path|with|invalid|chars" };

        foreach (var outputPath in invalidPaths)
        {
            // Arrange
            var settings = new AnalyzeCommand.Settings { OutputFile = outputPath };

            // Act
            var result = TestableAnalyzeCommand.TestValidateAnalyzeSettings(settings);

            // Assert
            result.IsValid.ShouldBeTrue();
        }
    }

    [Fact]
    public void AnalyzeCommand_ShouldAcceptEmptyOutputFilePath()
    {
        // Arrange
        var settings = new AnalyzeCommand.Settings { OutputFile = "" };

        // Act
        var result = TestableAnalyzeCommand.TestValidateAnalyzeSettings(settings);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void AnalyzeCommand_ShouldAcceptWhitespaceOutputFilePath()
    {
        // Arrange
        var settings = new AnalyzeCommand.Settings { OutputFile = "   " };

        // Act
        var result = TestableAnalyzeCommand.TestValidateAnalyzeSettings(settings);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void AnalyzeCommand_ShouldAcceptValidSettingsCombination()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var outputFile = Path.Combine(tempDir, "test.json");
        var settings = new AnalyzeCommand.Settings
        {
            Format = "json",
            OutputFile = outputFile,
            Verbose = true,
            ExcludePackages = new[] { "test.package1", "test.package2" },
        };

        // Act
        var result = TestableAnalyzeCommand.TestValidateAnalyzeSettings(settings);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    #endregion
}
