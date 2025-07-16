namespace NuGone.Domain.Features.PackageAnalysis.Entities;

/// <summary>
/// Represents a .NET project in the solution.
/// Core domain entity for project management as specified in RFC-0002.
/// </summary>
public class Project
{
    public Project(string filePath, string name, string targetFramework)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));

        if (string.IsNullOrWhiteSpace(targetFramework))
            throw new ArgumentException(
                "Target framework cannot be null or empty",
                nameof(targetFramework)
            );

        FilePath = filePath;
        Name = name;
        TargetFramework = targetFramework;
        PackageReferences = new List<PackageReference>();
        GlobalUsings = new List<GlobalUsing>();
        SourceFiles = new List<string>();
        ExcludePatterns = new List<string>();
    }

    /// <summary>
    /// The full path to the project file (.csproj, .vbproj, etc.).
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// The name of the project (typically the filename without extension).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The target framework of the project (e.g., net9.0, netstandard2.0).
    /// RFC-0002 specifies support for multi-targeted projects.
    /// </summary>
    public string TargetFramework { get; }

    /// <summary>
    /// Collection of package references in this project.
    /// </summary>
    public IList<PackageReference> PackageReferences { get; }

    /// <summary>
    /// Collection of global using declarations in this project.
    /// Global usings make package namespaces available throughout the project without explicit using statements.
    /// </summary>
    public IList<GlobalUsing> GlobalUsings { get; }

    /// <summary>
    /// Collection of source file paths in this project.
    /// Used for usage scanning as per RFC-0002.
    /// </summary>
    public IList<string> SourceFiles { get; }

    /// <summary>
    /// Patterns for files/folders to exclude from analysis.
    /// RFC-0002 specifies user-defined exclusion patterns.
    /// </summary>
    public IList<string> ExcludePatterns { get; }

    /// <summary>
    /// Gets the directory path of the project.
    /// </summary>
    public string DirectoryPath => Path.GetDirectoryName(FilePath) ?? string.Empty;

    /// <summary>
    /// Adds a package reference to the project.
    /// </summary>
    /// <param name="packageReference">The package reference to add</param>
    public void AddPackageReference(PackageReference packageReference)
    {
        if (packageReference == null)
            throw new ArgumentNullException(nameof(packageReference));

        if (!PackageReferences.Contains(packageReference))
            PackageReferences.Add(packageReference);
    }

    /// <summary>
    /// Removes a package reference from the project.
    /// </summary>
    /// <param name="packageReference">The package reference to remove</param>
    /// <returns>True if the package was removed, false if it wasn't found</returns>
    public bool RemovePackageReference(PackageReference packageReference)
    {
        if (packageReference == null)
            throw new ArgumentNullException(nameof(packageReference));

        return PackageReferences.Remove(packageReference);
    }

    /// <summary>
    /// Gets all unused package references in this project.
    /// </summary>
    /// <returns>Collection of unused package references</returns>
    public IEnumerable<PackageReference> GetUnusedPackages()
    {
        return PackageReferences.Where(p => !p.IsUsed);
    }

    /// <summary>
    /// Gets all used package references in this project.
    /// </summary>
    /// <returns>Collection of used package references</returns>
    public IEnumerable<PackageReference> GetUsedPackages()
    {
        return PackageReferences.Where(p => p.IsUsed);
    }

    /// <summary>
    /// Adds a global using declaration to the project.
    /// </summary>
    /// <param name="globalUsing">The global using declaration to add</param>
    public void AddGlobalUsing(GlobalUsing globalUsing)
    {
        if (globalUsing == null)
            throw new ArgumentNullException(nameof(globalUsing));

        if (!GlobalUsings.Contains(globalUsing))
            GlobalUsings.Add(globalUsing);
    }

    /// <summary>
    /// Removes a global using declaration from the project.
    /// </summary>
    /// <param name="globalUsing">The global using declaration to remove</param>
    /// <returns>True if the global using was removed, false if it wasn't found</returns>
    public bool RemoveGlobalUsing(GlobalUsing globalUsing)
    {
        if (globalUsing == null)
            throw new ArgumentNullException(nameof(globalUsing));

        return GlobalUsings.Remove(globalUsing);
    }

    /// <summary>
    /// Checks if a package has a corresponding global using declaration.
    /// </summary>
    /// <param name="packageId">The package identifier to check</param>
    /// <returns>True if the package has a global using declaration</returns>
    public bool HasGlobalUsingForPackage(string packageId)
    {
        if (string.IsNullOrWhiteSpace(packageId))
            return false;

        return GlobalUsings.Any(gu => gu.PackageId.Equals(packageId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Adds a source file to the project.
    /// </summary>
    /// <param name="filePath">The path to the source file</param>
    public void AddSourceFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        if (!SourceFiles.Contains(filePath))
            SourceFiles.Add(filePath);
    }

    /// <summary>
    /// Adds an exclusion pattern for files/folders to skip during analysis.
    /// </summary>
    /// <param name="pattern">The exclusion pattern</param>
    public void AddExcludePattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            throw new ArgumentException("Pattern cannot be null or empty", nameof(pattern));

        if (!ExcludePatterns.Contains(pattern))
            ExcludePatterns.Add(pattern);
    }

    /// <summary>
    /// Checks if a file path should be excluded from analysis based on the exclude patterns.
    /// </summary>
    /// <param name="filePath">The file path to check</param>
    /// <returns>True if the file should be excluded, false otherwise</returns>
    public bool ShouldExcludeFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return true;

        // Check against exclude patterns
        foreach (var pattern in ExcludePatterns)
        {
            if (IsFileMatchingPattern(filePath, pattern))
                return true;
        }

        // RFC-0002: Exclude known auto-generated files by default
        var fileName = Path.GetFileName(filePath);
        if (
            fileName.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase)
        )
        {
            return true;
        }

        return false;
    }

    private static bool IsFileMatchingPattern(string filePath, string pattern)
    {
        // Simple pattern matching - could be enhanced with more sophisticated glob patterns
        if (pattern.Contains("**"))
        {
            // Handle recursive directory patterns like **/Generated/**
            var parts = pattern.Split(["**"], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                var prefix = parts[0].TrimEnd('/');
                var suffix = parts[1].TrimStart('/');

                return filePath.Contains(prefix, StringComparison.OrdinalIgnoreCase)
                    && filePath.Contains(suffix, StringComparison.OrdinalIgnoreCase);
            }
        }

        // Simple wildcard matching
        return filePath.Contains(pattern.Replace("*", ""), StringComparison.OrdinalIgnoreCase);
    }

    public override string ToString()
    {
        return $"{Name} ({TargetFramework}) - {PackageReferences.Count} packages";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Project other)
            return false;

        return FilePath.Equals(other.FilePath, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return FilePath.ToLowerInvariant().GetHashCode();
    }
}
