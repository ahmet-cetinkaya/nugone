using System.IO.Abstractions.TestingHelpers;
using NuGone.Application.Features.PackageAnalysis.Services.Abstractions;
using NuGone.Cli.Features.AnalyzeCommand.Commands;
using NuGone.Cli.Shared.Constants;
using NuGone.Cli.Shared.Models;
using Spectre.Console.Cli;

namespace NuGone.Cli.Tests.Commands;

/// <summary>
/// Tests for AnalyzeCommand class.
/// Validates RFC-0001: CLI Architecture And Command Design - AnalyzeCommand implementation.
/// </summary>
public partial class AnalyzeCommandTests
{
    private readonly System.IO.Abstractions.IFileSystem _fileSystem;
    private readonly AnalyzeCommand _command;
    private readonly string _testProjectPath;
    private readonly string _testSolutionPath;

    public AnalyzeCommandTests()
    {
        _fileSystem = new MockFileSystem();
        _command = new AnalyzeCommand(_fileSystem);
        _testProjectPath = Path.Combine("test", "project", "Test.csproj");
        _testSolutionPath = Path.Combine("test", "solution", "Test.sln");

        // Setup test files
        ((MockFileSystem)_fileSystem).AddFile(
            _testProjectPath,
            new MockFileData("<Project></Project>")
        );
        ((MockFileSystem)_fileSystem).AddFile(
            _testSolutionPath,
            new MockFileData("Microsoft Visual Studio Solution File")
        );
    }

    #region Helper Classes

    /// <summary>
    /// Testable version of AnalyzeCommand that exposes protected methods for testing.
    /// </summary>
    private class TestableAnalyzeCommand(System.IO.Abstractions.IFileSystem fileSystem)
        : AnalyzeCommand(fileSystem)
    {
        public Result<string> TestValidateAndResolveProjectPath(string? projectPath)
        {
            return ValidateAndResolveProjectPath(projectPath);
        }

        public static bool TestIsVerboseMode(Settings settings)
        {
            return IsVerboseMode(settings);
        }

        public static bool TestIsJsonFormat(Settings settings)
        {
            return settings.Format?.ToLowerInvariant() == "json";
        }

        public static bool TestShouldShowSuccessMessage(Settings settings)
        {
            return settings.Format?.ToLowerInvariant() != "json" || settings.Verbose;
        }

        public static bool TestShouldShowProgressMessage(Settings settings)
        {
            return settings.Format?.ToLowerInvariant() != "json" || settings.Verbose;
        }

        // Method to access the static validation method for testing
        public static ValidationResult TestValidateAnalyzeSettings(Settings settings)
        {
            return AnalyzeCommand.ValidateAnalyzeSettings(settings);
        }

        // Override to prevent actual execution during tests
        protected override async Task<Result<int>> ExecuteCommandAsync(
            CommandContext context,
            Settings settings,
            CancellationToken cancellationToken = default
        )
        {
            await Task.CompletedTask;
            return ExitCodes.Success;
        }
    }

    #endregion
}
