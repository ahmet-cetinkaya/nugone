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
public partial class PackageUsageAnalyzer(
    IProjectRepository projectRepository,
    INuGetRepository nugetRepository,
    ILogger<PackageUsageAnalyzer> logger
) : IPackageUsageAnalyzer
{
    private readonly IProjectRepository _projectRepository =
        projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
    private readonly INuGetRepository _nugetRepository =
        nugetRepository ?? throw new ArgumentNullException(nameof(nugetRepository));
    private readonly ILogger<PackageUsageAnalyzer> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    // Regex patterns for detecting namespace usage
    private static readonly Regex UsingStatementRegex = MyRegex();

    // Regex for detecting namespace aliases (e.g., using PackageSerilog = Serilog;)
    private static readonly Regex UsingAliasRegex = MyRegex1();

    private static readonly Regex NamespaceUsageRegex = MyRegex2();

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

        // Step 2: Parse target frameworks for multi-target support (RFC-0002)
        var targetFrameworks = project.TargetFramework.Split(
            ';',
            StringSplitOptions.RemoveEmptyEntries
        );

        // RFC-0004: Track error-logged files at project level to avoid duplicate logging
        var errorLoggedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Step 3: For each package, get its namespaces and scan for usage
        foreach (var packageRef in project.PackageReferences)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogDebug(
                "Analyzing package: {PackageId} in project: {ProjectName}",
                packageRef.PackageId,
                project.Name
            );

            // Reset usage status before analysis
            packageRef.ResetUsageStatus();

            // Get namespaces for all target frameworks (RFC-0002: multi-target support)
            var allNamespaces = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var targetFramework in targetFrameworks)
            {
                var namespaces = await GetPackageNamespacesAsync(
                    packageRef.PackageId,
                    packageRef.Version,
                    targetFramework.Trim()
                );
                foreach (var ns in namespaces)
                {
                    _ = allNamespaces.Add(ns);
                }
            }

            _logger.LogDebug(
                "Package {PackageId} has {NamespaceCount} namespaces: {Namespaces}",
                packageRef.PackageId,
                allNamespaces.Count,
                string.Join(", ", allNamespaces)
            );

            if (allNamespaces.Count == 0)
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
                allNamespaces,
                project,
                errorLoggedFiles,
                cancellationToken
            );

            // If package has global using, also scan for implicit usage
            if (packageRef.HasGlobalUsing)
            {
                var globalUsageResults = await ScanSourceFilesForGlobalUsageAsync(
                    sourceFiles,
                    allNamespaces,
                    project,
                    errorLoggedFiles,
                    cancellationToken
                );

                // Merge global usage results with regular usage results
                foreach (var (namespaceName, usageLocations) in globalUsageResults)
                {
                    if (!usageResults.ContainsKey(namespaceName))
                        usageResults[namespaceName] = new List<string>();

                    foreach (var location in usageLocations)
                    {
                        if (!usageResults[namespaceName].Contains(location))
                            usageResults[namespaceName].Add(location);
                    }
                }
            }

            _logger.LogDebug(
                "Package {PackageId} usage scan found {UsageCount} namespace matches",
                packageRef.PackageId,
                usageResults.Sum(kvp => kvp.Value.Count)
            );

            // Mark package as used if any namespace usage was found
            foreach (var (namespaceName, usageLocations) in usageResults)
            {
                foreach (var location in usageLocations)
                {
                    packageRef.MarkAsUsed(location, namespaceName);
                }
            }

            _logger.LogDebug(
                "Package {PackageId} analysis result: IsUsed={IsUsed}, UsageLocations={LocationCount}",
                packageRef.PackageId,
                packageRef.IsUsed,
                packageRef.UsageLocations.Count
            );
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
    /// Scans source files for usage of specific package namespaces with project-aware exclusion.
    /// RFC-0002: Usage scanning with proper file exclusion patterns.
    /// </summary>
    public async Task<Dictionary<string, List<string>>> ScanSourceFilesForUsageAsync(
        IEnumerable<string> sourceFiles,
        IEnumerable<string> packageNamespaces,
        Project project,
        CancellationToken cancellationToken = default
    )
    {
        var errorLoggedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return await ScanSourceFilesForUsageAsync(
            sourceFiles,
            packageNamespaces,
            project,
            errorLoggedFiles,
            cancellationToken
        );
    }

    /// <summary>
    /// Scans source files for usage of specific package namespaces with project-aware exclusion and shared error tracking.
    /// RFC-0002: Usage scanning with proper file exclusion patterns.
    /// RFC-0004: Shared error tracking to prevent duplicate logging across package analyses.
    /// </summary>
    public async Task<Dictionary<string, List<string>>> ScanSourceFilesForUsageAsync(
        IEnumerable<string> sourceFiles,
        IEnumerable<string> packageNamespaces,
        Project project,
        HashSet<string> errorLoggedFiles,
        CancellationToken cancellationToken = default
    )
    {
        var usageResults = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var namespacePatterns = packageNamespaces.Select(ns => new NamespacePattern(ns)).ToList();

        foreach (var sourceFile in sourceFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Use project's proper file exclusion logic (RFC-0002: auto-generated files + patterns)
            if (project.ShouldExcludeFile(sourceFile))
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
                // RFC-0004: Avoid duplicate error logging for the same file across all package analyses
                if (!errorLoggedFiles.Contains(sourceFile))
                {
                    _logger.LogWarning(
                        ex,
                        "Error scanning file for namespace usage: {SourceFile}",
                        sourceFile
                    );
                    _ = errorLoggedFiles.Add(sourceFile);
                }
            }
        }

        return usageResults;
    }

    /// <summary>
    /// Scans source files for implicit usage of package namespaces through global usings.
    /// When a package has global usings, its namespaces are implicitly available without explicit using statements.
    /// </summary>
    public async Task<Dictionary<string, List<string>>> ScanSourceFilesForGlobalUsageAsync(
        IEnumerable<string> sourceFiles,
        IEnumerable<string> packageNamespaces,
        Project project,
        HashSet<string> errorLoggedFiles,
        CancellationToken cancellationToken = default
    )
    {
        var usageResults = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var namespacePatterns = packageNamespaces.Select(ns => new NamespacePattern(ns)).ToList();

        foreach (var sourceFile in sourceFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Use project's proper file exclusion logic
            if (project.ShouldExcludeFile(sourceFile))
                continue;

            try
            {
                var content = await _projectRepository.ReadSourceFileAsync(
                    sourceFile,
                    cancellationToken
                );
                var foundNamespaces = ScanFileForGlobalNamespaceUsage(content, namespacePatterns);

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
                // Avoid duplicate error logging for the same file across all package analyses
                if (!errorLoggedFiles.Contains(sourceFile))
                {
                    _logger.LogWarning(
                        ex,
                        "Error scanning file for global namespace usage: {SourceFile}",
                        sourceFile
                    );
                    _ = errorLoggedFiles.Add(sourceFile);
                }
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

        return errors.Count != 0 ? ValidationResult.Failure(errors) : ValidationResult.Success();
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
                    _ = foundNamespaces.Add(namespaceName);
                }
            }
        }

        // Scan for namespace aliases (e.g., using PackageSerilog = Serilog;)
        var aliasMatches = UsingAliasRegex.Matches(content);
        var namespaceAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in aliasMatches)
        {
            var aliasName = match.Groups[1].Value;
            var actualNamespace = match.Groups[2].Value;
            namespaceAliases[aliasName] = actualNamespace;

            // Check if the actual namespace matches any of our patterns
            foreach (var pattern in namespacePatterns)
            {
                if (pattern.Matches(actualNamespace))
                {
                    _ = foundNamespaces.Add(actualNamespace);
                }
            }
        }

        // Scan for direct namespace usage (e.g., System.Console.WriteLine)
        var usageMatches = NamespaceUsageRegex.Matches(content);
        foreach (Match match in usageMatches)
        {
            var fullQualifiedName = match.Groups[1].Value;

            // Check if this is an alias usage (e.g., PackageSerilog.ILogger)
            var firstPart = fullQualifiedName.Split('.')[0];
            if (namespaceAliases.TryGetValue(firstPart, out var actualNamespace))
            {
                // Replace the alias with the actual namespace
                var remainingParts = fullQualifiedName.Substring(firstPart.Length);
                var resolvedName = actualNamespace + remainingParts;

                // Check the resolved namespace
                var resolvedParts = resolvedName.Split('.');
                for (int i = 1; i <= resolvedParts.Length; i++)
                {
                    var namespaceName = string.Join(".", resolvedParts.Take(i));
                    foreach (var pattern in namespacePatterns)
                    {
                        if (pattern.Matches(namespaceName))
                        {
                            _ = foundNamespaces.Add(namespaceName);
                        }
                    }
                }
            }
            else
            {
                // Check all possible namespace prefixes of the qualified name
                // For "Complex.Package.Utilities.Helper", check:
                // - "Complex"
                // - "Complex.Package"
                // - "Complex.Package.Utilities"
                // - "Complex.Package.Utilities.Helper"
                var parts = fullQualifiedName.Split('.');
                for (int i = 1; i <= parts.Length; i++)
                {
                    var namespaceName = string.Join(".", parts.Take(i));
                    foreach (var pattern in namespacePatterns)
                    {
                        if (pattern.Matches(namespaceName))
                        {
                            _ = foundNamespaces.Add(namespaceName);
                        }
                    }
                }
            }
        }

        return foundNamespaces;
    }

    private static HashSet<string> ScanFileForGlobalNamespaceUsage(
        string content,
        IList<NamespacePattern> namespacePatterns
    )
    {
        var foundNamespaces = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // When a namespace is globally available, we need to look for usage patterns
        // that would indicate the namespace is being used without explicit using statements.

        // Scan for direct namespace usage (e.g., System.Console.WriteLine, Xunit.Assert.True)
        var usageMatches = NamespaceUsageRegex.Matches(content);
        foreach (Match match in usageMatches)
        {
            var fullQualifiedName = match.Groups[1].Value;

            // Check all possible namespace prefixes of the qualified name
            var parts = fullQualifiedName.Split('.');
            for (int i = 1; i <= parts.Length; i++)
            {
                var namespaceName = string.Join(".", parts.Take(i));
                foreach (var pattern in namespacePatterns)
                {
                    if (pattern.Matches(namespaceName))
                    {
                        _ = foundNamespaces.Add(namespaceName);
                    }
                }
            }
        }

        // For global usings, we also need to scan for unqualified usage patterns
        // that would only work if the namespace is globally available
        foreach (var pattern in namespacePatterns)
        {
            var namespaceName = pattern.Pattern;

            // Look for common patterns that indicate namespace usage without qualification
            // For Xunit namespace, look for [Fact], [Theory], Assert.*, etc.
            if (namespaceName.Equals("Xunit", StringComparison.OrdinalIgnoreCase))
            {
                // Look for Xunit attributes and classes
                if (
                    content.Contains("[Fact]")
                    || content.Contains("[Theory]")
                    || content.Contains("Assert.")
                    || content.Contains("[InlineData")
                )
                {
                    _ = foundNamespaces.Add(namespaceName);
                }
            }

            // For other namespaces, look for unqualified class/method usage patterns
            // This is a more general approach that looks for identifiers that could be from the namespace
            var unqualifiedPattern = new Regex(
                @"\b([A-Z][a-zA-Z0-9_]*)\s*[\.\(]",
                RegexOptions.Compiled
            );

            var unqualifiedMatches = unqualifiedPattern.Matches(content);
            foreach (Match match in unqualifiedMatches)
            {
                var identifier = match.Groups[1].Value;

                // This is a heuristic - if we find an identifier that could belong to the namespace
                // and there's no explicit using statement for it, it might be from a global using
                // We'll be conservative and only mark it as used if it's a common pattern
                if (IsLikelyFromNamespace(identifier, namespaceName))
                {
                    _ = foundNamespaces.Add(namespaceName);
                }
            }
        }

        return foundNamespaces;
    }

    private static bool IsLikelyFromNamespace(string identifier, string namespaceName)
    {
        // This is a heuristic method to determine if an identifier is likely from a specific namespace
        // For now, we'll implement some common patterns

        if (namespaceName.Equals("Xunit", StringComparison.OrdinalIgnoreCase))
        {
            return identifier.Equals("Assert", StringComparison.OrdinalIgnoreCase)
                || identifier.Equals("Fact", StringComparison.OrdinalIgnoreCase)
                || identifier.Equals("Theory", StringComparison.OrdinalIgnoreCase);
        }

        if (namespaceName.Equals("Moq", StringComparison.OrdinalIgnoreCase))
        {
            return identifier.Equals("Mock", StringComparison.OrdinalIgnoreCase)
                || identifier.Equals("It", StringComparison.OrdinalIgnoreCase);
        }

        // For other namespaces, we'll be conservative and not assume usage
        // This could be enhanced in the future with more sophisticated analysis
        return false;
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

    [GeneratedRegex(
        @"^\s*using\s+(?:global\s+)?([a-zA-Z_][a-zA-Z0-9_]*(?:\.[a-zA-Z_0-9][a-zA-Z0-9_]*)*)\s*;",
        RegexOptions.Multiline | RegexOptions.Compiled
    )]
    private static partial Regex MyRegex();

    [GeneratedRegex(
        @"^\s*using\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*=\s*([a-zA-Z_][a-zA-Z0-9_]*(?:\.[a-zA-Z_0-9][a-zA-Z0-9_]*)*)\s*;",
        RegexOptions.Multiline | RegexOptions.Compiled
    )]
    private static partial Regex MyRegex1();

    [GeneratedRegex(
        @"(?:new\s+)?([a-zA-Z_][a-zA-Z0-9_]*(?:\.[a-zA-Z_0-9][a-zA-Z0-9_]*)*)\s*[\.\(]",
        RegexOptions.Compiled
    )]
    private static partial Regex MyRegex2();
}
