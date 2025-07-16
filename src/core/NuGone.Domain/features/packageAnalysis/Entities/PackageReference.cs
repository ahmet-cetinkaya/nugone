namespace NuGone.Domain.Features.PackageAnalysis.Entities;

/// <summary>
/// Represents a NuGet package reference in a project.
/// Core domain entity for package analysis as specified in RFC-0002.
/// </summary>
public class PackageReference
{
    public PackageReference(
        string packageId,
        string version,
        string projectPath,
        bool isDirect = true,
        string? condition = null,
        bool hasGlobalUsing = false
    )
    {
        if (string.IsNullOrWhiteSpace(packageId))
            throw new ArgumentException("Package ID cannot be null or empty", nameof(packageId));

        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Version cannot be null or empty", nameof(version));

        if (string.IsNullOrWhiteSpace(projectPath))
            throw new ArgumentException(
                "Project path cannot be null or empty",
                nameof(projectPath)
            );

        PackageId = packageId;
        Version = version;
        ProjectPath = projectPath;
        IsDirect = isDirect;
        Condition = condition;
        HasGlobalUsing = hasGlobalUsing;
        IsUsed = false; // Default to unused until analysis proves otherwise
        UsageLocations = new List<string>();
        DetectedNamespaces = new List<string>();
    }

    /// <summary>
    /// The unique identifier of the NuGet package.
    /// </summary>
    public string PackageId { get; }

    /// <summary>
    /// The version of the package reference.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// The path to the project file containing this package reference.
    /// </summary>
    public string ProjectPath { get; }

    /// <summary>
    /// Indicates whether this is a direct dependency (true) or transitive dependency (false).
    /// RFC-0002 requires distinguishing between direct and transitive dependencies.
    /// </summary>
    public bool IsDirect { get; }

    /// <summary>
    /// Optional condition attribute from the PackageReference element.
    /// RFC-0002 specifies handling conditional references.
    /// </summary>
    public string? Condition { get; }

    /// <summary>
    /// Indicates whether this package has a corresponding global Using declaration.
    /// Global usings make package namespaces available throughout the project without explicit using statements.
    /// </summary>
    public bool HasGlobalUsing { get; }

    /// <summary>
    /// Indicates whether the package is detected as being used in the codebase.
    /// Set by the usage analysis algorithm.
    /// </summary>
    public bool IsUsed { get; private set; }

    /// <summary>
    /// List of file paths where usage of this package was detected.
    /// </summary>
    public IList<string> UsageLocations { get; }

    /// <summary>
    /// List of namespaces from this package that were detected in the codebase.
    /// </summary>
    public IList<string> DetectedNamespaces { get; }

    /// <summary>
    /// Marks the package as used and records the location where it was detected.
    /// </summary>
    /// <param name="filePath">The file path where usage was detected</param>
    /// <param name="namespace">The namespace that was used (optional)</param>
    public void MarkAsUsed(string filePath, string? @namespace = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        IsUsed = true;

        if (!UsageLocations.Contains(filePath))
            UsageLocations.Add(filePath);

        if (!string.IsNullOrWhiteSpace(@namespace) && !DetectedNamespaces.Contains(@namespace))
            DetectedNamespaces.Add(@namespace);
    }

    /// <summary>
    /// Resets the usage status of the package (useful for re-analysis).
    /// </summary>
    public void ResetUsageStatus()
    {
        IsUsed = false;
        UsageLocations.Clear();
        DetectedNamespaces.Clear();
    }

    public override string ToString()
    {
        return $"{PackageId} {Version} ({(IsDirect ? "Direct" : "Transitive")}) - {(IsUsed ? "Used" : "Unused")}";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not PackageReference other)
            return false;

        return PackageId.Equals(other.PackageId, StringComparison.OrdinalIgnoreCase)
            && Version.Equals(other.Version, StringComparison.OrdinalIgnoreCase)
            && ProjectPath.Equals(other.ProjectPath, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            PackageId.ToLowerInvariant(),
            Version.ToLowerInvariant(),
            ProjectPath.ToLowerInvariant()
        );
    }
}
