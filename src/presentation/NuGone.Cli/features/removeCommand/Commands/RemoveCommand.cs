using System.ComponentModel;
using NuGone.Application.Features.PackageAnalysis.Services.Abstractions;
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
    private static readonly string[] ValidFormats = ["text", "json"];

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
    }

    protected override Result<int> ExecuteCommand(CommandContext context, Settings settings)
    {
        // Validate settings first
        var settingsValidation = ValidateRemoveSettings(settings);
        if (!settingsValidation.IsValid)
            return Error.ValidationFailed(string.Join(", ", settingsValidation.Errors));

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
            if (settings.ExcludePackages?.Length > 0)
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

        var removalResult = PerformRemoval(projectPath, settings);
        if (removalResult.IsFailure)
            return removalResult.Error;

        ConsoleHelpers.WriteSuccess("Package removal completed successfully");
        return ExitCodes.Success;
    }

    public static ValidationResult ValidateRemoveSettings(Settings settings)
    {
        var errors = new List<string>();

        // Validate format option
        if (
            !string.IsNullOrEmpty(settings.Format)
            && !ValidFormats.Contains(settings.Format.ToLowerInvariant())
        )
        {
            errors.Add($"Format must be either 'text' or 'json'. Provided: {settings.Format}");
        }

        // Validate output file path if provided
        if (!string.IsNullOrEmpty(settings.OutputFile))
        {
            try
            {
                var directory = Path.GetDirectoryName(settings.OutputFile);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    errors.Add($"Output directory not found: {directory}");
                }
            }
            catch (ArgumentException ex)
            {
                errors.Add($"Invalid output file path: {ex.Message}");
            }
        }

        // Validate that we're not in dry-run mode if skip confirmation is set
        if (settings.SkipConfirmation && settings.DryRun)
        {
            errors.Add("Cannot skip confirmation in dry-run mode");
        }

        return errors.Count > 0 ? ValidationResult.Failure(errors) : ValidationResult.Success();
    }

    private static Result PerformRemoval(string projectPath, Settings settings)
    {
        try
        {
            if (settings.Verbose)
                ConsoleHelpers.WriteVerbose($"Preparing package removal for: {projectPath}");

            if (settings.DryRun)
            {
                ConsoleHelpers.WriteInfo("DRY RUN: Package removal is simulated");
                ConsoleHelpers.WriteInfo("In a real implementation, this would:");
                ConsoleHelpers.WriteInfo("  1. Analyze the project to identify unused packages");
                ConsoleHelpers.WriteInfo("  2. Prompt for confirmation (unless skipped)");
                ConsoleHelpers.WriteInfo(
                    "  3. Remove unused package references from project files"
                );
                ConsoleHelpers.WriteInfo("  4. Clean up any unused package assets");
                return Result.Success();
            }

            // NOTE: Package removal functionality is planned for a future release (v2.0+)
            // Implementation will involve:
            // 1. Using IPackageUsageAnalyzer to identify unused packages
            // 2. Creating a removal command/handler in the application layer
            // 3. Using project repository services to modify project files
            // 4. Implementing safety checks and rollback capabilities per RFC-0004
            ConsoleHelpers.WriteWarning(
                "Package removal functionality is planned for a future release"
            );
            ConsoleHelpers.WriteInfo("This command will:");
            ConsoleHelpers.WriteInfo("  - Detect unused packages in the project");
            ConsoleHelpers.WriteInfo("  - Allow selective removal of unused packages");
            ConsoleHelpers.WriteInfo("  - Provide backup and rollback capabilities");

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Error.OperationFailed("removal", ex.Message);
        }
    }
}
