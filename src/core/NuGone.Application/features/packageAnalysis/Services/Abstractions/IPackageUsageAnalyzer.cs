using NuGone.Domain.Features.PackageAnalysis.Entities;

namespace NuGone.Application.Features.PackageAnalysis.Services.Abstractions;

/// <summary>
/// Core interface for package usage analysis.
/// Defines the contract for detecting unused NuGet packages as specified in RFC-0002.
/// </summary>
public interface IPackageUsageAnalyzer
{
    /// <summary>
    /// Analyzes package usage across all projects in a solution.
    /// RFC-0002: Core algorithm for detecting unused packages.
    /// </summary>
    /// <param name="solution">The solution to analyze</param>
    /// <param name="cancellationToken">Cancellation token for long-running operations</param>
    /// <returns>Task representing the analysis operation</returns>
    Task AnalyzePackageUsageAsync(Solution solution, CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes package usage for a specific project.
    /// RFC-0002: Project-level analysis for targeted scanning.
    /// </summary>
    /// <param name="project">The project to analyze</param>
    /// <param name="cancellationToken">Cancellation token for long-running operations</param>
    /// <returns>Task representing the analysis operation</returns>
    Task AnalyzeProjectPackageUsageAsync(
        Project project,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Scans source files for usage of specific package namespaces.
    /// RFC-0002: Usage scanning for using statements and class/method names.
    /// </summary>
    /// <param name="sourceFiles">Collection of source file paths to scan</param>
    /// <param name="packageNamespaces">Namespaces to look for</param>
    /// <param name="excludePatterns">Patterns for files to exclude from scanning</param>
    /// <param name="cancellationToken">Cancellation token for long-running operations</param>
    /// <returns>Dictionary mapping namespace to list of files where it was found</returns>
    Task<Dictionary<string, List<string>>> ScanSourceFilesForUsageAsync(
        IEnumerable<string> sourceFiles,
        IEnumerable<string> packageNamespaces,
        IEnumerable<string> excludePatterns,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Scans source files for usage of specific package namespaces with project-aware exclusion.
    /// RFC-0002: Usage scanning with proper file exclusion patterns.
    /// </summary>
    /// <param name="sourceFiles">Collection of source file paths to scan</param>
    /// <param name="packageNamespaces">Namespaces to look for in the source files</param>
    /// <param name="project">Project containing exclusion patterns and auto-generated file detection</param>
    /// <param name="cancellationToken">Cancellation token for long-running operations</param>
    /// <returns>Dictionary mapping found namespaces to their usage locations</returns>
    Task<Dictionary<string, List<string>>> ScanSourceFilesForUsageAsync(
        IEnumerable<string> sourceFiles,
        IEnumerable<string> packageNamespaces,
        Project project,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Scans source files for usage of specific package namespaces with project-aware exclusion and shared error tracking.
    /// RFC-0002: Usage scanning with proper file exclusion patterns.
    /// RFC-0004: Shared error tracking to prevent duplicate logging across package analyses.
    /// </summary>
    /// <param name="sourceFiles">Collection of source file paths to scan</param>
    /// <param name="packageNamespaces">Namespaces to look for in the source files</param>
    /// <param name="project">Project containing exclusion patterns and auto-generated file detection</param>
    /// <param name="errorLoggedFiles">Shared set to track files that have already had errors logged</param>
    /// <param name="cancellationToken">Cancellation token for long-running operations</param>
    /// <returns>Dictionary mapping found namespaces to their usage locations</returns>
    Task<Dictionary<string, List<string>>> ScanSourceFilesForUsageAsync(
        IEnumerable<string> sourceFiles,
        IEnumerable<string> packageNamespaces,
        Project project,
        HashSet<string> errorLoggedFiles,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Validates that all required inputs are present and accessible.
    /// RFC-0002: Input validation for paths and configuration.
    /// </summary>
    /// <param name="solution">The solution to validate</param>
    /// <returns>Validation result indicating success or failure with details</returns>
    Task<ValidationResult> ValidateInputsAsync(Solution solution);

    /// <summary>
    /// Gets the namespaces associated with a specific package.
    /// Used to determine what to look for during usage scanning.
    /// </summary>
    /// <param name="packageId">The package identifier</param>
    /// <param name="version">The package version</param>
    /// <param name="targetFramework">The target framework</param>
    /// <returns>Collection of namespaces provided by the package</returns>
    Task<IEnumerable<string>> GetPackageNamespacesAsync(
        string packageId,
        string version,
        string targetFramework
    );
}

/// <summary>
/// Represents the result of input validation.
/// </summary>
public class ValidationResult(bool isValid, IEnumerable<string>? errors = null)
{
    /// <summary>
    /// Indicates whether the validation passed.
    /// </summary>
    public bool IsValid { get; } = isValid;

    /// <summary>
    /// Collection of validation error messages.
    /// </summary>
    public IList<string> Errors { get; } = errors?.ToList() ?? new List<string>();

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>Valid ValidationResult</returns>
    public static ValidationResult Success() => new(true);

    /// <summary>
    /// Creates a failed validation result with error messages.
    /// </summary>
    /// <param name="errors">Collection of error messages</param>
    /// <returns>Invalid ValidationResult</returns>
    public static ValidationResult Failure(params string[] errors) => new(false, errors);

    /// <summary>
    /// Creates a failed validation result with error messages.
    /// </summary>
    /// <param name="errors">Collection of error messages</param>
    /// <returns>Invalid ValidationResult</returns>
    public static ValidationResult Failure(IEnumerable<string> errors) => new(false, errors);
}
