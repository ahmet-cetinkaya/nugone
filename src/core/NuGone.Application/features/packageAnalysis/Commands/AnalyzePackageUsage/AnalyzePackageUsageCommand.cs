namespace NuGone.Application.Features.PackageAnalysis.Commands.AnalyzePackageUsage;

/// <summary>
/// Command for analyzing package usage in a solution or project.
/// RFC-0002: Input parameters for unused package detection.
/// </summary>
public class AnalyzePackageUsageCommand
{
    public AnalyzePackageUsageCommand(string path)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        ExcludePatterns = new List<string>();
        IncludeTransitiveDependencies = false;
        Verbose = false;
        DryRun = true;
        TimeoutSeconds = 300;
    }

    /// <summary>
    /// Path to the solution file, project file, or directory to analyze.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Patterns for files/folders to exclude from analysis.
    /// RFC-0002: User-defined exclusion patterns.
    /// </summary>
    public IList<string> ExcludePatterns { get; }

    /// <summary>
    /// Whether to include transitive dependencies in the analysis.
    /// RFC-0002: Transitive dependency analysis option.
    /// </summary>
    public bool IncludeTransitiveDependencies { get; set; }

    /// <summary>
    /// Whether to enable verbose output with detailed information.
    /// </summary>
    public bool Verbose { get; set; }

    /// <summary>
    /// Optional target framework to filter analysis.
    /// If not specified, all target frameworks are analyzed.
    /// </summary>
    public string? TargetFramework { get; set; }

    /// <summary>
    /// Whether to perform a dry run without making any changes.
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// Timeout for the analysis operation in seconds.
    /// Default is 300 seconds (5 minutes).
    /// </summary>
    public int TimeoutSeconds { get; set; }

    /// <summary>
    /// Adds an exclusion pattern.
    /// </summary>
    /// <param name="pattern">The pattern to exclude</param>
    public void AddExcludePattern(string pattern)
    {
        if (!string.IsNullOrWhiteSpace(pattern) && !ExcludePatterns.Contains(pattern))
            ExcludePatterns.Add(pattern);
    }

    /// <summary>
    /// Adds multiple exclusion patterns.
    /// </summary>
    /// <param name="patterns">The patterns to exclude</param>
    public void AddExcludePatterns(IEnumerable<string> patterns)
    {
        foreach (var pattern in patterns)
            AddExcludePattern(pattern);
    }
}
