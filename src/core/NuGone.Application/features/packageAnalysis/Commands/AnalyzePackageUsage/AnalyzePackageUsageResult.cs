namespace NuGone.Application.Features.PackageAnalysis.Commands.AnalyzePackageUsage;

/// <summary>
/// Result for package usage analysis command.
/// RFC-0002: Output data for unused package detection results.
/// </summary>
public class AnalyzePackageUsageResult
{
    public AnalyzePackageUsageResult(
        string analyzedPath,
        TimeSpan analysisTime,
        IEnumerable<ProjectAnalysisResult> projectResults
    )
    {
        AnalyzedPath = analyzedPath ?? throw new ArgumentNullException(nameof(analyzedPath));
        AnalysisTime = analysisTime;
        ProjectResults =
            projectResults?.ToList() ?? throw new ArgumentNullException(nameof(projectResults));

        // Calculate summary statistics
        TotalProjects = ProjectResults.Count;
        TotalPackages = ProjectResults.Sum(p => p.TotalPackages);
        UnusedPackages = ProjectResults.Sum(p => p.UnusedPackages);
        UsedPackages = ProjectResults.Sum(p => p.UsedPackages);
    }

    /// <summary>
    /// The path that was analyzed (solution, project, or directory).
    /// </summary>
    public string AnalyzedPath { get; }

    /// <summary>
    /// Time taken to complete the analysis.
    /// </summary>
    public TimeSpan AnalysisTime { get; }

    /// <summary>
    /// Results for each project that was analyzed.
    /// </summary>
    public IList<ProjectAnalysisResult> ProjectResults { get; }

    /// <summary>
    /// Total number of projects analyzed.
    /// </summary>
    public int TotalProjects { get; }

    /// <summary>
    /// Total number of packages across all projects.
    /// </summary>
    public int TotalPackages { get; }

    /// <summary>
    /// Total number of unused packages across all projects.
    /// </summary>
    public int UnusedPackages { get; }

    /// <summary>
    /// Total number of used packages across all projects.
    /// </summary>
    public int UsedPackages { get; }

    /// <summary>
    /// Percentage of packages that are unused.
    /// </summary>
    public double UnusedPercentage =>
        TotalPackages > 0 ? (double)UnusedPackages / TotalPackages * 100 : 0;

    /// <summary>
    /// Gets all unused package details across all projects.
    /// </summary>
    /// <returns>Collection of unused package details</returns>
    public IEnumerable<PackageUsageDetail> GetAllUnusedPackages()
    {
        return ProjectResults.SelectMany(p => p.UnusedPackageDetails);
    }

    /// <summary>
    /// Gets all used package details across all projects.
    /// </summary>
    /// <returns>Collection of used package details</returns>
    public IEnumerable<PackageUsageDetail> GetAllUsedPackages()
    {
        return ProjectResults.SelectMany(p => p.UsedPackageDetails);
    }

    /// <summary>
    /// Gets unused packages grouped by package ID.
    /// Useful for identifying packages that are unused across multiple projects.
    /// </summary>
    /// <returns>Dictionary with package ID as key and list of usage details as value</returns>
    public Dictionary<string, List<PackageUsageDetail>> GetUnusedPackagesGroupedById()
    {
        return GetAllUnusedPackages()
            .GroupBy(p => p.PackageId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the analysis found any unused packages.
    /// </summary>
    /// <returns>True if there are unused packages, false otherwise</returns>
    public bool HasUnusedPackages() => UnusedPackages > 0;

    /// <summary>
    /// Gets a summary string of the analysis results.
    /// </summary>
    /// <returns>Summary string</returns>
    public string GetSummary()
    {
        return $"Analyzed {TotalProjects} project(s) with {TotalPackages} package(s). "
            + $"Found {UnusedPackages} unused package(s) ({UnusedPercentage:F1}%) and {UsedPackages} used package(s).";
    }
}

/// <summary>
/// Analysis result for a single project.
/// </summary>
public class ProjectAnalysisResult
{
    public ProjectAnalysisResult(
        string projectName,
        string projectPath,
        string targetFramework,
        IEnumerable<PackageUsageDetail> unusedPackageDetails,
        IEnumerable<PackageUsageDetail> usedPackageDetails
    )
    {
        ProjectName = projectName ?? throw new ArgumentNullException(nameof(projectName));
        ProjectPath = projectPath ?? throw new ArgumentNullException(nameof(projectPath));
        TargetFramework =
            targetFramework ?? throw new ArgumentNullException(nameof(targetFramework));
        UnusedPackageDetails =
            unusedPackageDetails?.ToList()
            ?? throw new ArgumentNullException(nameof(unusedPackageDetails));
        UsedPackageDetails =
            usedPackageDetails?.ToList()
            ?? throw new ArgumentNullException(nameof(usedPackageDetails));

        UnusedPackages = UnusedPackageDetails.Count;
        UsedPackages = UsedPackageDetails.Count;
        TotalPackages = UnusedPackages + UsedPackages;
    }

    /// <summary>
    /// Name of the project.
    /// </summary>
    public string ProjectName { get; }

    /// <summary>
    /// Full path to the project file.
    /// </summary>
    public string ProjectPath { get; }

    /// <summary>
    /// Target framework of the project.
    /// </summary>
    public string TargetFramework { get; }

    /// <summary>
    /// Details of unused packages in this project.
    /// </summary>
    public IList<PackageUsageDetail> UnusedPackageDetails { get; }

    /// <summary>
    /// Details of used packages in this project.
    /// </summary>
    public IList<PackageUsageDetail> UsedPackageDetails { get; }

    /// <summary>
    /// Number of unused packages in this project.
    /// </summary>
    public int UnusedPackages { get; }

    /// <summary>
    /// Number of used packages in this project.
    /// </summary>
    public int UsedPackages { get; }

    /// <summary>
    /// Total number of packages in this project.
    /// </summary>
    public int TotalPackages { get; }

    /// <summary>
    /// Percentage of packages that are unused in this project.
    /// </summary>
    public double UnusedPercentage =>
        TotalPackages > 0 ? (double)UnusedPackages / TotalPackages * 100 : 0;
}

/// <summary>
/// Detailed information about a package's usage.
/// </summary>
public class PackageUsageDetail(
    string packageId,
    string version,
    bool isDirect,
    bool isUsed,
    string? condition = null,
    IEnumerable<string>? usageLocations = null,
    IEnumerable<string>? detectedNamespaces = null,
    bool hasGlobalUsing = false
)
{
    /// <summary>
    /// The package identifier.
    /// </summary>
    public string PackageId { get; } =
        packageId ?? throw new ArgumentNullException(nameof(packageId));

    /// <summary>
    /// The package version.
    /// </summary>
    public string Version { get; } = version ?? throw new ArgumentNullException(nameof(version));

    /// <summary>
    /// Whether this is a direct dependency.
    /// </summary>
    public bool IsDirect { get; } = isDirect;

    /// <summary>
    /// Whether the package is used in the codebase.
    /// </summary>
    public bool IsUsed { get; } = isUsed;

    /// <summary>
    /// Optional condition from the PackageReference.
    /// </summary>
    public string? Condition { get; } = condition;

    /// <summary>
    /// File paths where usage was detected.
    /// </summary>
    public IList<string> UsageLocations { get; } = usageLocations?.ToList() ?? new List<string>();

    /// <summary>
    /// Namespaces from this package that were detected.
    /// </summary>
    public IList<string> DetectedNamespaces { get; } =
        detectedNamespaces?.ToList() ?? new List<string>();

    /// <summary>
    /// Whether this package has a corresponding global using declaration.
    /// Global usings make package namespaces available throughout the project without explicit using statements.
    /// </summary>
    public bool HasGlobalUsing { get; } = hasGlobalUsing;

    /// <summary>
    /// Gets a display string for the package.
    /// </summary>
    /// <returns>Display string</returns>
    public string GetDisplayString()
    {
        var dependencyType = IsDirect ? "Direct" : "Transitive";
        var usageStatus = IsUsed ? "Used" : "Unused";
        var conditionText = !string.IsNullOrWhiteSpace(Condition)
            ? $" (Condition: {Condition})"
            : "";
        var globalUsingText = HasGlobalUsing ? " [Global Using]" : "";

        return $"{PackageId} {Version} ({dependencyType}, {usageStatus}){conditionText}{globalUsingText}";
    }
}
