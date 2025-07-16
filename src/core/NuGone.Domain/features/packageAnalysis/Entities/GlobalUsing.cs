namespace NuGone.Domain.Features.PackageAnalysis.Entities;

/// <summary>
/// Represents a global using declaration in a project file.
/// Global usings make package namespaces available throughout the project without explicit using statements.
/// </summary>
public class GlobalUsing
{
    public GlobalUsing(string packageId, string projectPath, string? condition = null)
    {
        if (string.IsNullOrWhiteSpace(packageId))
            throw new ArgumentException("Package ID cannot be null or empty", nameof(packageId));

        if (string.IsNullOrWhiteSpace(projectPath))
            throw new ArgumentException(
                "Project path cannot be null or empty",
                nameof(projectPath)
            );

        PackageId = packageId;
        ProjectPath = projectPath;
        Condition = condition;
    }

    /// <summary>
    /// The package identifier referenced in the global using declaration.
    /// </summary>
    public string PackageId { get; }

    /// <summary>
    /// The path to the project file containing this global using declaration.
    /// </summary>
    public string ProjectPath { get; }

    /// <summary>
    /// Optional condition attribute from the Using element.
    /// </summary>
    public string? Condition { get; }

    public override string ToString()
    {
        return $"Global Using: {PackageId}";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not GlobalUsing other)
            return false;

        return PackageId.Equals(other.PackageId, StringComparison.OrdinalIgnoreCase)
            && ProjectPath.Equals(other.ProjectPath, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(PackageId.ToLowerInvariant(), ProjectPath.ToLowerInvariant());
    }
}
