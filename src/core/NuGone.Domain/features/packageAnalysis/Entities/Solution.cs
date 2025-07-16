namespace NuGone.Domain.Features.PackageAnalysis.Entities;

/// <summary>
/// Represents a .NET solution containing multiple projects.
/// Core domain entity for solution management as specified in RFC-0002.
/// </summary>
public class Solution
{
    public Solution(string filePath, string name, bool isVirtual = false)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));

        FilePath = filePath;
        Name = name;
        IsVirtual = isVirtual;
        Projects = new List<Project>();
        CentralPackageManagementEnabled = false;
        DirectoryPackagesPropsPath = null;
    }

    /// <summary>
    /// The full path to the solution file (.sln or .slnx).
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// The name of the solution (typically the filename without extension).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Indicates whether this is a virtual solution (created from individual projects).
    /// Virtual solutions don't have an actual .sln file on disk.
    /// </summary>
    public bool IsVirtual { get; }

    /// <summary>
    /// Collection of projects in this solution.
    /// </summary>
    public IList<Project> Projects { get; }

    /// <summary>
    /// Indicates whether central package management is enabled for this solution.
    /// RFC-0002 specifies support for central package management.
    /// </summary>
    public bool CentralPackageManagementEnabled { get; private set; }

    /// <summary>
    /// Path to the Directory.Packages.props file if central package management is enabled.
    /// </summary>
    public string? DirectoryPackagesPropsPath { get; private set; }

    /// <summary>
    /// Gets the directory path of the solution.
    /// </summary>
    public string DirectoryPath => Path.GetDirectoryName(FilePath) ?? string.Empty;

    /// <summary>
    /// Adds a project to the solution.
    /// </summary>
    /// <param name="project">The project to add</param>
    public void AddProject(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        if (!Projects.Contains(project))
            Projects.Add(project);
    }

    /// <summary>
    /// Removes a project from the solution.
    /// </summary>
    /// <param name="project">The project to remove</param>
    /// <returns>True if the project was removed, false if it wasn't found</returns>
    public bool RemoveProject(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        return Projects.Remove(project);
    }

    /// <summary>
    /// Enables central package management for this solution.
    /// </summary>
    /// <param name="directoryPackagesPropsPath">Path to the Directory.Packages.props file</param>
    public void EnableCentralPackageManagement(string directoryPackagesPropsPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPackagesPropsPath))
            throw new ArgumentException(
                "Directory.Packages.props path cannot be null or empty",
                nameof(directoryPackagesPropsPath)
            );

        CentralPackageManagementEnabled = true;
        DirectoryPackagesPropsPath = directoryPackagesPropsPath;
    }

    /// <summary>
    /// Disables central package management for this solution.
    /// </summary>
    public void DisableCentralPackageManagement()
    {
        CentralPackageManagementEnabled = false;
        DirectoryPackagesPropsPath = null;
    }

    /// <summary>
    /// Gets all package references across all projects in the solution.
    /// </summary>
    /// <returns>Collection of all package references</returns>
    public IEnumerable<PackageReference> GetAllPackageReferences()
    {
        return Projects.SelectMany(p => p.PackageReferences);
    }

    /// <summary>
    /// Gets all unused package references across all projects in the solution.
    /// </summary>
    /// <returns>Collection of unused package references</returns>
    public IEnumerable<PackageReference> GetAllUnusedPackages()
    {
        return Projects.SelectMany(p => p.GetUnusedPackages());
    }

    /// <summary>
    /// Gets all used package references across all projects in the solution.
    /// </summary>
    /// <returns>Collection of used package references</returns>
    public IEnumerable<PackageReference> GetAllUsedPackages()
    {
        return Projects.SelectMany(p => p.GetUsedPackages());
    }

    /// <summary>
    /// Gets package references grouped by package ID across all projects.
    /// Useful for identifying duplicate package references with different versions.
    /// </summary>
    /// <returns>Dictionary with package ID as key and list of package references as value</returns>
    public Dictionary<string, List<PackageReference>> GetPackageReferencesGroupedById()
    {
        return GetAllPackageReferences()
            .GroupBy(p => p.PackageId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets statistics about package usage in the solution.
    /// </summary>
    /// <returns>Tuple containing total packages, used packages, and unused packages counts</returns>
    public (int Total, int Used, int Unused) GetPackageStatistics()
    {
        var allPackages = GetAllPackageReferences().ToList();
        var usedCount = allPackages.Count(p => p.IsUsed);
        var unusedCount = allPackages.Count(p => !p.IsUsed);

        return (allPackages.Count, usedCount, unusedCount);
    }

    /// <summary>
    /// Finds a project by its file path.
    /// </summary>
    /// <param name="projectPath">The path to the project file</param>
    /// <returns>The project if found, null otherwise</returns>
    public Project? FindProjectByPath(string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            return null;

        return Projects.FirstOrDefault(p =>
            p.FilePath.Equals(projectPath, StringComparison.OrdinalIgnoreCase)
        );
    }

    /// <summary>
    /// Finds a project by its name.
    /// </summary>
    /// <param name="projectName">The name of the project</param>
    /// <returns>The project if found, null otherwise</returns>
    public Project? FindProjectByName(string projectName)
    {
        if (string.IsNullOrWhiteSpace(projectName))
            return null;

        return Projects.FirstOrDefault(p =>
            p.Name.Equals(projectName, StringComparison.OrdinalIgnoreCase)
        );
    }

    public override string ToString()
    {
        return $"{Name} - {Projects.Count} projects, {GetAllPackageReferences().Count()} packages";
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Solution other)
            return false;

        return FilePath.Equals(other.FilePath, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return FilePath.ToLowerInvariant().GetHashCode();
    }
}
