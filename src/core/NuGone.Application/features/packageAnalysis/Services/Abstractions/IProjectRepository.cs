using NuGone.Domain.Features.PackageAnalysis.Entities;

namespace NuGone.Application.Features.PackageAnalysis.Services.Abstractions;

/// <summary>
/// Repository interface for project-related operations.
/// Defines the contract for accessing project files and metadata as specified in RFC-0002.
/// </summary>
public interface IProjectRepository
{
    /// <summary>
    /// Discovers all project files in a given directory and its subdirectories.
    /// RFC-0002: Project discovery for solution analysis.
    /// </summary>
    /// <param name="rootPath">The root directory to search</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of project file paths</returns>
    Task<IEnumerable<string>> DiscoverProjectFilesAsync(
        string rootPath,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Loads project information from a project file.
    /// RFC-0002: Project file parsing for metadata extraction.
    /// </summary>
    /// <param name="projectFilePath">Path to the project file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Project entity with metadata</returns>
    Task<Project> LoadProjectAsync(
        string projectFilePath,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets all source files for a project.
    /// RFC-0002: Source file discovery for usage scanning.
    /// </summary>
    /// <param name="project">The project to get source files for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of source file paths</returns>
    Task<IEnumerable<string>> GetProjectSourceFilesAsync(
        Project project,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Reads the content of a source file.
    /// RFC-0002: File content access for usage analysis.
    /// </summary>
    /// <param name="filePath">Path to the source file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Content of the file</returns>
    Task<string> ReadSourceFileAsync(
        string filePath,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Checks if a file or directory exists.
    /// </summary>
    /// <param name="path">Path to check</param>
    /// <returns>True if the path exists, false otherwise</returns>
    Task<bool> ExistsAsync(string path);

    /// <summary>
    /// Gets the directory path of a file.
    /// </summary>
    /// <param name="filePath">The file path</param>
    /// <returns>Directory path</returns>
    string GetDirectoryPath(string filePath);

    /// <summary>
    /// Combines path segments into a full path.
    /// </summary>
    /// <param name="paths">Path segments to combine</param>
    /// <returns>Combined path</returns>
    string CombinePaths(params string[] paths);
}
