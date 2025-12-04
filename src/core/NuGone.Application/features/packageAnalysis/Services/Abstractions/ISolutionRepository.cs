using NuGone.Domain.Features.PackageAnalysis.Entities;

namespace NuGone.Application.Features.PackageAnalysis.Services.Abstractions;

/// <summary>
/// Repository interface for solution-related operations.
/// Defines the contract for accessing solution files and metadata as specified in RFC-0002.
/// </summary>
public interface ISolutionRepository
{
    /// <summary>
    /// Discovers solution files in a given directory.
    /// RFC-0002: Solution discovery for analysis.
    /// </summary>
    /// <param name="rootPath">The root directory to search</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of solution file paths</returns>
    Task<IEnumerable<string>> DiscoverSolutionFilesAsync(
        string rootPath,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Loads solution information from a solution file.
    /// RFC-0002: Solution file parsing for metadata extraction.
    /// </summary>
    /// <param name="solutionFilePath">Path to the solution file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Solution entity with metadata</returns>
    Task<Solution> LoadSolutionAsync(
        string solutionFilePath,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Loads central package versions from Directory.Packages.props.
    /// RFC-0002: Central package version resolution.
    /// </summary>
    /// <param name="directoryPackagesPropsPath">Path to the Directory.Packages.props file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of package versions</returns>
    Task<Dictionary<string, string>> LoadCentralPackageVersionsAsync(
        string directoryPackagesPropsPath,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Resolves the full path to a project file relative to the solution directory.
    /// </summary>
    /// <param name="solutionDirectoryPath">Path to the solution directory</param>
    /// <param name="relativeProjectPath">Relative path to the project file</param>
    /// <returns>Full path to the project file</returns>
    string ResolveProjectPath(string solutionDirectoryPath, string relativeProjectPath);
}
