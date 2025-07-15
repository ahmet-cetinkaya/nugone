using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NuGone.Application.Features.PackageAnalysis.Services.Abstractions;
using NuGone.Domain.Features.PackageAnalysis.Entities;
using NuGone.Domain.Features.PackageAnalysis.ValueObjects;

namespace NuGone.Application.Features.PackageAnalysis.Services;

/// <summary>
/// Core implementation of the package usage analyzer.
/// Implements the unused package detection algorithm as specified in RFC-0002.
/// </summary>
public class PackageUsageAnalyzer : IPackageUsageAnalyzer
{
    private readonly IProjectRepository _projectRepository;
    private readonly INuGetRepository _nugetRepository;
    private readonly ILogger<PackageUsageAnalyzer> _logger;

    // Regex patterns for detecting namespace usage
    private static readonly Regex UsingStatementRegex = new(
        @"^\s*using\s+([a-zA-Z_][a-zA-Z0-9_]*(?:\.[a-zA-Z_][a-zA-Z0-9_]*)*)\s*;",
        RegexOptions.Compiled | RegexOptions.Multiline
    );

    private static readonly Regex NamespaceUsageRegex = new(
        @"\b([a-zA-Z_][a-zA-Z0-9_]*(?:\.[a-zA-Z_][a-zA-Z0-9_]*)*)\.",
        RegexOptions.Compiled
    );

    public PackageUsageAnalyzer(
        IProjectRepository projectRepository,
        INuGetRepository nugetRepository,
        ILogger<PackageUsageAnalyzer> logger
    )
    {
        _projectRepository =
            projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        _nugetRepository =
            nugetRepository ?? throw new ArgumentNullException(nameof(nugetRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Analyzes package usage across all projects in a solution.
    /// RFC-0002: Core algorithm for detecting unused packages.
    /// </summary>
    public async Task AnalyzePackageUsageAsync(
        Solution solution,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation(
            "Starting package usage analysis for solution: {SolutionName}",
            solution.Name
        );

        foreach (var project in solution.Projects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await AnalyzeProjectPackageUsageAsync(project, cancellationToken);
        }

        _logger.LogInformation(
            "Completed package usage analysis for solution: {SolutionName}",
            solution.Name
        );
    }

    /// <summary>
    /// Analyzes package usage for a specific project.
    /// RFC-0002: Project-level analysis for targeted scanning.
    /// </summary>
    public async Task AnalyzeProjectPackageUsageAsync(
        Project project,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogDebug("Analyzing package usage for project: {ProjectName}", project.Name);

        // Step 1: Get all source files for the project
        var sourceFiles = await _projectRepository.GetProjectSourceFilesAsync(
            project,
            cancellationToken
        );

        // Step 2: For each package, get its namespaces and scan for usage
        foreach (var packageRef in project.PackageReferences)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Reset usage status before analysis
            packageRef.ResetUsageStatus();

            // Get namespaces provided by this package
            var namespaces = await GetPackageNamespacesAsync(
                packageRef.PackageId,
                packageRef.Version,
                project.TargetFramework
            );

            if (!namespaces.Any())
            {
                _logger.LogWarning(
                    "No namespaces found for package: {PackageId}",
                    packageRef.PackageId
                );
                continue;
            }

            // Scan source files for usage of these namespaces
            var usageResults = await ScanSourceFilesForUsageAsync(
                sourceFiles,
                namespaces,
                project.ExcludePatterns,
                cancellationToken
            );

            // Mark package as used if any namespace usage was found
            foreach (var (namespaceName, usageLocations) in usageResults)
            {
                foreach (var location in usageLocations)
                {
                    packageRef.MarkAsUsed(location, namespaceName);
                }
            }
        }

        var usedCount = project.PackageReferences.Count(p => p.IsUsed);
        var unusedCount = project.PackageReferences.Count(p => !p.IsUsed);

        _logger.LogDebug(
            "Project {ProjectName}: {UsedCount} used, {UnusedCount} unused packages",
            project.Name,
            usedCount,
            unusedCount
        );
    }

    /// <summary>
    /// Scans source files for usage of specific package namespaces.
    /// RFC-0002: Usage scanning for using statements and class/method names.
    /// </summary>
    public async Task<Dictionary<string, List<string>>> ScanSourceFilesForUsageAsync(
        IEnumerable<string> sourceFiles,
        IEnumerable<string> packageNamespaces,
        IEnumerable<string> excludePatterns,
        CancellationToken cancellationToken = default
    )
    {
        var usageResults = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var namespacePatterns = packageNamespaces.Select(ns => new NamespacePattern(ns)).ToList();

        foreach (var sourceFile in sourceFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Skip files that match exclude patterns
            if (ShouldExcludeFile(sourceFile, excludePatterns))
                continue;

            try
            {
                var content = await _projectRepository.ReadSourceFileAsync(
                    sourceFile,
                    cancellationToken
                );
                var foundNamespaces = ScanFileForNamespaceUsage(content, namespacePatterns);

                foreach (var foundNamespace in foundNamespaces)
                {
                    if (!usageResults.ContainsKey(foundNamespace))
                        usageResults[foundNamespace] = new List<string>();

                    if (!usageResults[foundNamespace].Contains(sourceFile))
                        usageResults[foundNamespace].Add(sourceFile);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Error scanning file for namespace usage: {SourceFile}",
                    sourceFile
                );
            }
        }

        return usageResults;
    }

    /// <summary>
    /// Validates that all required inputs are present and accessible.
    /// RFC-0002: Input validation for paths and configuration.
    /// </summary>
    public async Task<ValidationResult> ValidateInputsAsync(Solution solution)
    {
        var errors = new List<string>();

        if (solution == null)
        {
            errors.Add("Solution cannot be null");
            return ValidationResult.Failure(errors);
        }

        // Validate solution file exists (skip for virtual solutions)
        if (!solution.IsVirtual && !await _projectRepository.ExistsAsync(solution.FilePath))
            errors.Add($"Solution file does not exist: {solution.FilePath}");

        // Validate each project
        foreach (var project in solution.Projects)
        {
            if (!await _projectRepository.ExistsAsync(project.FilePath))
                errors.Add($"Project file does not exist: {project.FilePath}");

            if (!await _projectRepository.ExistsAsync(project.DirectoryPath))
                errors.Add($"Project directory does not exist: {project.DirectoryPath}");
        }

        return errors.Any() ? ValidationResult.Failure(errors) : ValidationResult.Success();
    }

    /// <summary>
    /// Gets the namespaces associated with a specific package.
    /// Used to determine what to look for during usage scanning.
    /// </summary>
    public async Task<IEnumerable<string>> GetPackageNamespacesAsync(
        string packageId,
        string version,
        string targetFramework
    )
    {
        return await _nugetRepository.GetPackageNamespacesAsync(
            packageId,
            version,
            targetFramework
        );
    }

    private static HashSet<string> ScanFileForNamespaceUsage(
        string content,
        IList<NamespacePattern> namespacePatterns
    )
    {
        var foundNamespaces = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Scan for using statements
        var usingMatches = UsingStatementRegex.Matches(content);
        foreach (Match match in usingMatches)
        {
            var namespaceName = match.Groups[1].Value;
            foreach (var pattern in namespacePatterns)
            {
                if (pattern.Matches(namespaceName))
                {
                    foundNamespaces.Add(namespaceName);
                }
            }
        }

        // Scan for direct namespace usage (e.g., System.Console.WriteLine)
        var usageMatches = NamespaceUsageRegex.Matches(content);
        foreach (Match match in usageMatches)
        {
            var namespaceName = match.Groups[1].Value;
            foreach (var pattern in namespacePatterns)
            {
                if (pattern.Matches(namespaceName))
                {
                    foundNamespaces.Add(namespaceName);
                }
            }
        }

        return foundNamespaces;
    }

    private static bool ShouldExcludeFile(string filePath, IEnumerable<string> excludePatterns)
    {
        foreach (var pattern in excludePatterns)
        {
            if (filePath.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
