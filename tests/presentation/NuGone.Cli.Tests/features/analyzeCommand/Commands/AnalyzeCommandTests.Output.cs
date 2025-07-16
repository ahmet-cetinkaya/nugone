using System.IO.Abstractions;
using Moq;
using NuGone.Cli.Features.AnalyzeCommand.Commands;
using Shouldly;
using Xunit;

namespace NuGone.Cli.Tests.Commands;

/// <summary>
/// Output formatting tests for AnalyzeCommand class.
/// Validates RFC-0001: CLI Architecture And Command Design - AnalyzeCommand output behavior.
/// </summary>
public partial class AnalyzeCommandTests
{
    #region Output Format Tests

    [Fact]
    public void AnalyzeCommand_ShouldShowSuccessMessageForTextFormat()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        var command = new TestableAnalyzeCommand(mockFileSystem.Object);
        var settings = new AnalyzeCommand.Settings { Format = "text", Verbose = false };

        // Act
        var shouldShowMessage = command.TestShouldShowSuccessMessage(settings);

        // Assert
        shouldShowMessage.ShouldBeTrue();
    }

    [Fact]
    public void AnalyzeCommand_ShouldNotShowSuccessMessageForJsonFormatWithoutVerbose()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        var command = new TestableAnalyzeCommand(mockFileSystem.Object);
        var settings = new AnalyzeCommand.Settings { Format = "json", Verbose = false };

        // Act
        var shouldShowMessage = command.TestShouldShowSuccessMessage(settings);

        // Assert
        shouldShowMessage.ShouldBeFalse();
    }

    [Fact]
    public void AnalyzeCommand_ShouldShowSuccessMessageForJsonFormatWithVerbose()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        var command = new TestableAnalyzeCommand(mockFileSystem.Object);
        var settings = new AnalyzeCommand.Settings { Format = "json", Verbose = true };

        // Act
        var shouldShowMessage = command.TestShouldShowSuccessMessage(settings);

        // Assert
        shouldShowMessage.ShouldBeTrue();
    }

    [Fact]
    public void AnalyzeCommand_ShouldShowProgressMessageForTextFormat()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        var command = new TestableAnalyzeCommand(mockFileSystem.Object);
        var settings = new AnalyzeCommand.Settings { Format = "text", Verbose = false };

        // Act
        var shouldShowProgress = command.TestShouldShowProgressMessage(settings);

        // Assert
        shouldShowProgress.ShouldBeTrue();
    }

    [Fact]
    public void AnalyzeCommand_ShouldNotShowProgressMessageForJsonFormatWithoutVerbose()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        var command = new TestableAnalyzeCommand(mockFileSystem.Object);
        var settings = new AnalyzeCommand.Settings { Format = "json", Verbose = false };

        // Act
        var shouldShowProgress = command.TestShouldShowProgressMessage(settings);

        // Assert
        shouldShowProgress.ShouldBeFalse();
    }

    [Fact]
    public void AnalyzeCommand_ShouldShowProgressMessageForJsonFormatWithVerbose()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        var command = new TestableAnalyzeCommand(mockFileSystem.Object);
        var settings = new AnalyzeCommand.Settings { Format = "json", Verbose = true };

        // Act
        var shouldShowProgress = command.TestShouldShowProgressMessage(settings);

        // Assert
        shouldShowProgress.ShouldBeTrue();
    }

    #endregion
}
