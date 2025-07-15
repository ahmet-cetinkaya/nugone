using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuGone.Application.Features.PackageAnalysis.Commands.AnalyzePackageUsage;
using NuGone.Application.Shared.Extensions;
using NuGone.Cli.Shared.Constants;
using NuGone.Cli.Shared.Models;
using NuGone.Cli.Shared.Utilities;
using NuGone.FileSystem.Extensions;
using NuGone.NuGet.Extensions;
using Spectre.Console.Cli;

namespace NuGone.Cli.Features.AnalyzeCommand.Commands;

/// <summary>
/// CLI command for analyzing unused packages.
/// Implements RFC-0001: CLI Architecture And Command Design.
/// </summary>
public class AnalyzeCommand
    : BaseCommand<AnalyzeCommand.Settings>,
        IAsyncCommand<AnalyzeCommand.Settings>
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

    protected override async Task<Result<int>> ExecuteCommandAsync(
        CommandContext context,
        Settings settings
    )
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

        // Show verbose info and progress messages (except for JSON format without verbose)
        if (settings.Format?.ToLowerInvariant() != "json" || settings.Verbose)
        {
            if (settings.Verbose)
            {
                ConsoleHelpers.WriteVerbose($"Analyzing project: {projectPath}");
                ConsoleHelpers.WriteVerbose($"Output format: {settings.Format ?? "text"}");
                if (settings.ExcludePackages?.Any() == true)
                {
                    ConsoleHelpers.WriteVerbose(
                        $"Excluded packages: {string.Join(", ", settings.ExcludePackages)}"
                    );
                }
            }

            if (settings.Format?.ToLowerInvariant() != "json" || settings.Verbose)
            {
                ConsoleHelpers.WriteInfo("Starting package analysis...");
            }
        }

        // Perform the actual package analysis using the CQRS handler
        var analysisResult = await PerformAnalysisAsync(projectPath, settings);
        if (analysisResult.IsFailure)
            return analysisResult.Error;

        // Display results
        DisplayResults(analysisResult.Value, settings);

        // Show success message for non-JSON formats or when verbose is enabled
        if (settings.Format?.ToLowerInvariant() != "json" || settings.Verbose)
        {
            ConsoleHelpers.WriteSuccess("Analysis completed successfully");
        }

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

    private async Task<Result<AnalyzePackageUsageResult>> PerformAnalysisAsync(
        string projectPath,
        Settings settings
    )
    {
        try
        {
            // Show progress message for non-JSON formats or when verbose is enabled
            if (settings.Format?.ToLowerInvariant() != "json" || settings.Verbose)
            {
                ConsoleHelpers.WriteInfo($"Analyzing packages in: {projectPath}");
            }

            // Set up dependency injection
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddApplicationServices();
            services.AddFileSystemServices();
            services.AddNuGetServices();

            using var serviceProvider = services.BuildServiceProvider();
            var handler = serviceProvider.GetRequiredService<AnalyzePackageUsageHandler>();

            // Create the command
            var command = new AnalyzePackageUsageCommand(projectPath);
            command.Verbose = settings.Verbose;
            command.DryRun = true; // Always dry run for analyze command

            // Add exclude patterns
            if (settings.ExcludePackages?.Any() == true)
            {
                command.AddExcludePatterns(settings.ExcludePackages);
            }

            // Execute the analysis
            var result = await handler.HandleAsync(command);
            if (result.IsFailure)
            {
                return Result<AnalyzePackageUsageResult>.Failure(
                    Error.OperationFailed("analysis", result.Error.Message)
                );
            }

            return Result<AnalyzePackageUsageResult>.Success(result.Value);
        }
        catch (Exception ex)
        {
            return Result<AnalyzePackageUsageResult>.Failure(
                Error.OperationFailed("analysis", ex.Message)
            );
        }
    }

    private void DisplayResults(AnalyzePackageUsageResult result, Settings settings)
    {
        // Display results based on format
        if (settings.Format?.ToLowerInvariant() == "json")
        {
            // For JSON format, show verbose info if requested, then output JSON
            if (settings.Verbose)
            {
                ConsoleHelpers.WriteInfo(
                    $"Analysis completed in {result.AnalysisTime.TotalSeconds:F2} seconds"
                );
                ConsoleHelpers.WriteInfo(result.GetSummary());
            }

            var jsonOutput = SerializeToJson(result);
            Console.WriteLine(jsonOutput);
        }
        else
        {
            // For text format, show verbose info if requested
            if (settings.Verbose)
            {
                ConsoleHelpers.WriteInfo(
                    $"Analysis completed in {result.AnalysisTime.TotalSeconds:F2} seconds"
                );
                ConsoleHelpers.WriteInfo(result.GetSummary());
            }

            DisplayTextResults(result, settings);
        }

        // Save to file if specified
        if (settings.OutputFile != null)
        {
            SaveResultsToFile(result, settings);
        }
    }

    private void DisplayTextResults(AnalyzePackageUsageResult result, Settings settings)
    {
        if (result.HasUnusedPackages())
        {
            ConsoleHelpers.WriteWarning($"Found {result.UnusedPackages} unused package(s):");

            foreach (var project in result.ProjectResults.Where(p => p.UnusedPackages > 0))
            {
                ConsoleHelpers.WriteInfo($"Project: {project.ProjectName}");
                foreach (var package in project.UnusedPackageDetails)
                {
                    ConsoleHelpers.WriteWarning($"  - {package.GetDisplayString()}");
                }
            }
        }
        else
        {
            ConsoleHelpers.WriteSuccess("No unused packages found!");
        }
    }

    private void SaveResultsToFile(AnalyzePackageUsageResult result, Settings settings)
    {
        try
        {
            var content =
                settings.Format?.ToLowerInvariant() == "json"
                    ? SerializeToJson(result)
                    : SerializeToText(result);

            File.WriteAllText(settings.OutputFile!, content);

            // Show save message for non-JSON console output or when verbose is enabled
            if (settings.Format?.ToLowerInvariant() != "json" || settings.Verbose)
            {
                ConsoleHelpers.WriteSuccess($"Results saved to: {settings.OutputFile}");
            }
        }
        catch (Exception ex)
        {
            // Always show error messages
            ConsoleHelpers.WriteError($"Failed to save results: {ex.Message}");
        }
    }

    private string SerializeToJson(AnalyzePackageUsageResult result)
    {
        var json = new
        {
            AnalyzedPath = result.AnalyzedPath,
            AnalysisTime = new
            {
                TotalSeconds = result.AnalysisTime.TotalSeconds,
                Formatted = $"{result.AnalysisTime.TotalSeconds:F2}s",
            },
            Summary = new
            {
                TotalProjects = result.TotalProjects,
                TotalPackages = result.TotalPackages,
                UnusedPackages = result.UnusedPackages,
                UsedPackages = result.UsedPackages,
                UnusedPercentage = Math.Round(result.UnusedPercentage, 1),
            },
            Projects = result.ProjectResults.Select(p => new
            {
                ProjectName = p.ProjectName,
                ProjectPath = p.ProjectPath,
                TargetFramework = p.TargetFramework,
                PackageCounts = new
                {
                    Total = p.TotalPackages,
                    Used = p.UsedPackages,
                    Unused = p.UnusedPackages,
                    UnusedPercentage = Math.Round(p.UnusedPercentage, 1),
                },
                UnusedPackages = p.UnusedPackageDetails.Select(pkg => new
                {
                    PackageId = pkg.PackageId,
                    Version = pkg.Version,
                    IsDirect = pkg.IsDirect,
                    Condition = pkg.Condition,
                    UsageLocations = pkg.UsageLocations,
                    DetectedNamespaces = pkg.DetectedNamespaces,
                }),
                UsedPackages = p.UsedPackageDetails.Select(pkg => new
                {
                    PackageId = pkg.PackageId,
                    Version = pkg.Version,
                    IsDirect = pkg.IsDirect,
                    Condition = pkg.Condition,
                    UsageLocations = pkg.UsageLocations,
                    DetectedNamespaces = pkg.DetectedNamespaces,
                }),
            }),
        };

        return System.Text.Json.JsonSerializer.Serialize(
            json,
            new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            }
        );
    }

    private string SerializeToText(AnalyzePackageUsageResult result)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Package Analysis Results");
        sb.AppendLine($"=======================");
        sb.AppendLine($"Analyzed Path: {result.AnalyzedPath}");
        sb.AppendLine($"Analysis Time: {result.AnalysisTime}");
        sb.AppendLine($"Summary: {result.GetSummary()}");
        sb.AppendLine();

        foreach (var project in result.ProjectResults)
        {
            sb.AppendLine($"Project: {project.ProjectName} ({project.TargetFramework})");
            sb.AppendLine($"Path: {project.ProjectPath}");

            if (project.UnusedPackages > 0)
            {
                sb.AppendLine($"Unused Packages ({project.UnusedPackages}):");
                foreach (var package in project.UnusedPackageDetails)
                {
                    sb.AppendLine($"  - {package.GetDisplayString()}");
                }
            }
            else
            {
                sb.AppendLine("No unused packages found.");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
