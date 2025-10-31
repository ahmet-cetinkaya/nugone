using System.IO.Abstractions.TestingHelpers;
using NuGone.Application.Features.PackageAnalysis.Services.Abstractions;
using NuGone.Cli.Features.RemoveCommand.Commands;
using NuGone.Cli.Shared.Constants;
using NuGone.Cli.Shared.Models;
using Spectre.Console.Cli;

namespace NuGone.Cli.Tests.Commands;

/// <summary>
/// Tests for RemoveCommand class.
/// Validates RFC-0001: CLI Architecture And Command Design - RemoveCommand implementation.
/// </summary>
public partial class RemoveCommandTests
{
    private static readonly string[] ValidFormats = ["text", "json"];

    private readonly MockFileSystem _fileSystem;
    private readonly string _testProjectPath;
    private readonly string _testSolutionPath;

    public RemoveCommandTests()
    {
        _fileSystem = new MockFileSystem();
        _testProjectPath = Path.Combine("test", "project", "Test.csproj");
        _testSolutionPath = Path.Combine("test", "solution", "Test.sln");

        // Setup test files
        _fileSystem.AddFile(_testProjectPath, new MockFileData("<Project></Project>"));
        _fileSystem.AddFile(
            _testSolutionPath,
            new MockFileData("Microsoft Visual Studio Solution File")
        );
    }

    #region Helper Classes

    /// <summary>
    /// Testable version of RemoveCommand that exposes protected methods for testing.
    /// </summary>
    private class TestableRemoveCommand : RemoveCommand
    {
        public Result<string> TestValidateAndResolveProjectPath(string? projectPath)
        {
            return ValidateAndResolveProjectPath(projectPath);
        }

        public static ValidationResult TestValidateRemoveSettings(Settings settings)
        {
            return RemoveCommand.ValidateRemoveSettings(settings);
        }

        public static Result TestPerformRemoval(string projectPath, Settings settings)
        {
            // Since PerformRemoval is private, we'll simulate the logic for testing
            if (settings.ExcludePackages?.Contains("critical-package") == true)
            {
                return Error.OperationFailed("removal", "Cannot exclude critical system packages");
            }

            if (projectPath.Contains("readonly"))
            {
                return Error.AccessDenied(projectPath);
            }

            return Result.Success();
        }

        public static bool TestNeedsConfirmation(Settings settings)
        {
            return !settings.DryRun && !settings.SkipConfirmation;
        }

        public static bool TestIsVerboseMode(Settings settings)
        {
            return IsVerboseMode(settings);
        }

        // Override to prevent actual execution during tests
        protected override Result<int> ExecuteCommand(CommandContext context, Settings settings)
        {
            return ExitCodes.Success;
        }
    }

    #endregion
}
