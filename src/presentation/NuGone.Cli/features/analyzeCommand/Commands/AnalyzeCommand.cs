using System.ComponentModel;
using NuGone.Cli.Shared.Constants;
using NuGone.Cli.Shared.Models;
using NuGone.Cli.Shared.Utilities;
using Spectre.Console.Cli;

namespace NuGone.Cli.Features.AnalyzeCommand.Commands;

/// <summary>
/// CLI command for analyzing unused packages.
/// Implements RFC-0001: CLI Architecture And Command Design.
/// </summary>
public class AnalyzeCommand : BaseCommand<AnalyzeCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("Path to project or solution")]
        [CommandOption("--project|-p")]
        public string? ProjectPath { get; init; }

        [Description("Only analyze packages, don't perform any modifications")]
        [CommandOption("--dry-run")]
        public bool DryRun { get; init; } = true; // Analyze is always dry-run by nature

        [Description("Output format: json or text")]
        [CommandOption("--format|-f")]
        public string Format { get; init; } = "text";

        [Description("Write report to file")]
        [CommandOption("--output|-o")]
        public string? OutputFile { get; init; }

        [Description("Exclude packages from analysis")]
        [CommandOption("--exclude")]
        public string[]? ExcludePackages { get; init; }

        [Description("Enable verbose output")]
        [CommandOption("--verbose|-v")]
        public bool Verbose { get; init; }

        // TODO: Add validation logic when ValidationResult is properly imported
    }

    protected override Result<int> ExecuteCommand(CommandContext context, Settings settings)
    {
        // Validate settings first
        var settingsValidation = ValidateAnalyzeSettings(settings);
        if (settingsValidation.IsFailure)
            return settingsValidation.Error;

        // Validate and resolve project path using base class method
        var projectPathResult = ValidateAndResolveProjectPath(settings.ProjectPath);
        if (projectPathResult.IsFailure)
            return projectPathResult.Error;

        var projectPath = projectPathResult.Value;

        if (settings.Verbose)
        {
            ConsoleHelpers.WriteVerbose($"Analyzing project: {projectPath}");
            ConsoleHelpers.WriteVerbose($"Output format: {settings.Format}");
            if (settings.ExcludePackages?.Any() == true)
            {
                ConsoleHelpers.WriteVerbose(
                    $"Excluded packages: {string.Join(", ", settings.ExcludePackages)}"
                );
            }
        }

        ConsoleHelpers.WriteInfo("Starting package analysis...");

        // TODO: Implement actual package analysis logic
        // This is a placeholder for RFC-0001 CLI architecture demonstration
        var analysisResult = PerformAnalysis(projectPath, settings);
        if (analysisResult.IsFailure)
            return analysisResult.Error;

        ConsoleHelpers.WriteSuccess("Analysis completed successfully");
        return ExitCodes.Success;
    }

    private Result ValidateAnalyzeSettings(Settings settings)
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

        return Result.Success();
    }

    private Result PerformAnalysis(string projectPath, Settings settings)
    {
        try
        {
            // Placeholder for actual analysis logic
            ConsoleHelpers.WriteInfo($"Analyzing packages in: {projectPath}");

            // Simulate some analysis work
            if (settings.ExcludePackages?.Contains("invalid-package") == true)
            {
                return Error.OperationFailed("analysis", "Invalid package name in exclusion list");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            // Convert unexpected exceptions to errors where appropriate
            return Error.OperationFailed("analysis", ex.Message);
        }
    }
}
