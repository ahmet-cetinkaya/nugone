using System.ComponentModel;
using NuGone.Cli.Shared.Constants;
using NuGone.Cli.Shared.Models;
using NuGone.Cli.Shared.Utilities;
using Spectre.Console.Cli;

namespace NuGone.Cli.Features.RemoveCommand.Commands;

/// <summary>
/// CLI command for removing unused packages.
/// Implements RFC-0001: CLI Architecture And Command Design.
/// </summary>
public class RemoveCommand : BaseCommand<RemoveCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("Path to project or solution")]
        [CommandOption("--project|-p")]
        public string? ProjectPath { get; init; }

        [Description("Exclude packages from removal")]
        [CommandOption("--exclude")]
        public string[]? ExcludePackages { get; init; }

        [Description("Perform a dry run without making actual changes")]
        [CommandOption("--dry-run")]
        public bool DryRun { get; init; }

        [Description("Skip confirmation prompts")]
        [CommandOption("--yes|-y")]
        public bool SkipConfirmation { get; init; }

        [Description("Output format: json or text")]
        [CommandOption("--format|-f")]
        public string Format { get; init; } = "text";

        [Description("Write report to file")]
        [CommandOption("--output|-o")]
        public string? OutputFile { get; init; }

        [Description("Enable verbose output")]
        [CommandOption("--verbose|-v")]
        public bool Verbose { get; init; }

        // TODO: Add validation logic when ValidationResult is properly imported
    }

    protected override Result<int> ExecuteCommand(CommandContext context, Settings settings)
    {
        // Validate settings first
        var settingsValidation = ValidateRemoveSettings(settings);
        if (settingsValidation.IsFailure)
            return settingsValidation.Error;

        // Validate and resolve project path using base class method
        var projectPathResult = ValidateAndResolveProjectPath(settings.ProjectPath);
        if (projectPathResult.IsFailure)
            return projectPathResult.Error;

        var projectPath = projectPathResult.Value;

        if (settings.Verbose)
        {
            ConsoleHelpers.WriteVerbose($"Removing packages from: {projectPath}");
            ConsoleHelpers.WriteVerbose($"Dry run: {settings.DryRun}");
            ConsoleHelpers.WriteVerbose($"Output format: {settings.Format}");
            if (settings.ExcludePackages?.Any() == true)
            {
                ConsoleHelpers.WriteVerbose(
                    $"Excluded packages: {string.Join(", ", settings.ExcludePackages)}"
                );
            }
        }

        if (settings.DryRun)
        {
            ConsoleHelpers.WriteInfo("Running in dry-run mode - no changes will be made");
        }
        else if (!settings.SkipConfirmation)
        {
            ConsoleHelpers.WriteWarning("This will remove unused packages from your project(s)");
            if (!ConsoleHelpers.Confirm("Do you want to continue?"))
            {
                ConsoleHelpers.WriteInfo("Operation cancelled by user");
                return ExitCodes.Success;
            }
        }

        ConsoleHelpers.WriteInfo("Starting package removal...");

        // TODO: Implement actual package removal logic
        var removalResult = PerformRemoval(projectPath, settings);
        if (removalResult.IsFailure)
            return removalResult.Error;

        ConsoleHelpers.WriteSuccess("Package removal completed successfully");
        return ExitCodes.Success;
    }

    private Result ValidateRemoveSettings(Settings settings)
    {
        // Validate format option
        if (
            !string.IsNullOrEmpty(settings.Format)
            && !new[] { "text", "json" }.Contains(settings.Format.ToLowerInvariant())
        )
        {
            return Error.ValidationFailed(
                "Format must be either 'text' or 'json'",
                new Dictionary<string, object> { ["ProvidedFormat"] = settings.Format }
            );
        }

        // Validate output file path if provided
        if (!string.IsNullOrEmpty(settings.OutputFile))
        {
            try
            {
                var directory = Path.GetDirectoryName(settings.OutputFile);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    return Error.DirectoryNotFound(directory);
                }
            }
            catch (ArgumentException ex)
            {
                return Error.InvalidArgument(
                    $"Invalid output file path: {ex.Message}",
                    "outputFile"
                );
            }
        }

        // Validate that we're not in dry-run mode if skip confirmation is set
        if (settings.SkipConfirmation && settings.DryRun)
        {
            return Error.ValidationFailed("Cannot skip confirmation in dry-run mode");
        }

        return Result.Success();
    }

    private Result PerformRemoval(string projectPath, Settings settings)
    {
        try
        {
            // Placeholder for actual removal logic
            ConsoleHelpers.WriteInfo($"Removing packages from: {projectPath}");

            // Simulate some removal work with potential failures
            if (settings.ExcludePackages?.Contains("critical-package") == true)
            {
                return Error.OperationFailed("removal", "Cannot exclude critical system packages");
            }

            // Simulate a file access issue
            if (projectPath.Contains("readonly"))
            {
                return Error.AccessDenied(projectPath);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            // Convert unexpected exceptions to errors where appropriate
            return Error.OperationFailed("removal", ex.Message);
        }
    }
}
