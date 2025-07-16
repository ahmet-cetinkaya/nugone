using System.IO.Abstractions;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using NuGone.Application.Features.PackageAnalysis.Services.Abstractions;
using NuGone.Domain.Features.PackageAnalysis.Entities;

namespace NuGone.FileSystem.Repositories;

/// <summary>
/// File system implementation of the project repository.
/// Handles project file discovery and parsing as specified in RFC-0002.
/// </summary>
public class ProjectRepository(IFileSystem fileSystem, ILogger<ProjectRepository> logger)
    : IProjectRepository
{
    private readonly IFileSystem _fileSystem =
        fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    private readonly ILogger<ProjectRepository> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private static readonly string[] ProjectFileExtensions = [".csproj", ".vbproj", ".fsproj"];
    private static readonly string[] SourceFileExtensions = [".cs", ".vb", ".fs"];

    /// <summary>
    /// Discovers all project files in a given directory and its subdirectories.
    /// RFC-0002: Project discovery for solution analysis.
    /// </summary>
    public async Task<IEnumerable<string>> DiscoverProjectFilesAsync(
        string rootPath,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogDebug("Discovering project files in: {RootPath}", rootPath);

        if (!await ExistsAsync(rootPath))
        {
            _logger.LogWarning("Root path does not exist: {RootPath}", rootPath);
            return Enumerable.Empty<string>();
        }

        var projectFiles = new List<string>();

        try
        {
            var directoryInfo = _fileSystem.DirectoryInfo.New(rootPath);
            var allFiles = directoryInfo.GetFiles("*", SearchOption.AllDirectories);

            foreach (var file in allFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (IsProjectFile(file.Extension))
                {
                    projectFiles.Add(file.FullName);
                    _logger.LogDebug("Found project file: {ProjectFile}", file.FullName);
                }
            }
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            _logger.LogError(ex, "Error discovering project files in: {RootPath}", rootPath);
            throw;
        }

        _logger.LogInformation(
            "Discovered {Count} project file(s) in: {RootPath}",
            projectFiles.Count,
            rootPath
        );
        return projectFiles;
    }

    /// <summary>
    /// Loads project information from a project file.
    /// RFC-0002: Project file parsing for metadata extraction.
    /// </summary>
    public async Task<Project> LoadProjectAsync(
        string projectFilePath,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogDebug("Loading project: {ProjectFilePath}", projectFilePath);

        if (!await ExistsAsync(projectFilePath))
            throw new FileNotFoundException($"Project file not found: {projectFilePath}");

        try
        {
            var content = await ReadSourceFileAsync(projectFilePath, cancellationToken);
            var document = XDocument.Parse(content);

            var projectName = _fileSystem.Path.GetFileNameWithoutExtension(projectFilePath);
            var targetFramework = ExtractTargetFramework(document);

            var project = new Project(projectFilePath, projectName, targetFramework);

            // Add default exclude patterns
            project.AddExcludePattern("**/bin/**");
            project.AddExcludePattern("**/obj/**");
            project.AddExcludePattern("**/.vs/**");
            project.AddExcludePattern("**/.git/**");

            _logger.LogDebug(
                "Loaded project: {ProjectName} ({TargetFramework})",
                projectName,
                targetFramework
            );
            return project;
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            _logger.LogError(ex, "Error loading project: {ProjectFilePath}", projectFilePath);
            throw;
        }
    }

    /// <summary>
    /// Gets all source files for a project.
    /// RFC-0002: Source file discovery for usage scanning.
    /// </summary>
    public async Task<IEnumerable<string>> GetProjectSourceFilesAsync(
        Project project,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogDebug("Getting source files for project: {ProjectName}", project.Name);

        var projectDirectory = GetDirectoryPath(project.FilePath);
        if (!await ExistsAsync(projectDirectory))
        {
            _logger.LogWarning(
                "Project directory does not exist: {ProjectDirectory}",
                projectDirectory
            );
            return Enumerable.Empty<string>();
        }

        var sourceFiles = new List<string>();

        try
        {
            var directoryInfo = _fileSystem.DirectoryInfo.New(projectDirectory);
            var allFiles = directoryInfo.GetFiles("*", SearchOption.AllDirectories);

            foreach (var file in allFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (IsSourceFile(file.Extension) && !project.ShouldExcludeFile(file.FullName))
                {
                    sourceFiles.Add(file.FullName);
                    project.AddSourceFile(file.FullName);
                }
            }
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            _logger.LogError(
                ex,
                "Error getting source files for project: {ProjectName}",
                project.Name
            );
            throw;
        }

        _logger.LogDebug(
            "Found {Count} source file(s) for project: {ProjectName}",
            sourceFiles.Count,
            project.Name
        );
        return sourceFiles;
    }

    /// <summary>
    /// Reads the content of a source file.
    /// RFC-0002: File content access for usage analysis.
    /// </summary>
    public async Task<string> ReadSourceFileAsync(
        string filePath,
        CancellationToken cancellationToken = default
    )
    {
        if (!await ExistsAsync(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        try
        {
            return await _fileSystem.File.ReadAllTextAsync(filePath, cancellationToken);
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            _logger.LogError(ex, "Error reading file: {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>
    /// Checks if a file or directory exists.
    /// </summary>
    public Task<bool> ExistsAsync(string path)
    {
        var exists = _fileSystem.File.Exists(path) || _fileSystem.Directory.Exists(path);
        return Task.FromResult(exists);
    }

    /// <summary>
    /// Gets the directory path of a file.
    /// </summary>
    public string GetDirectoryPath(string filePath)
    {
        return _fileSystem.Path.GetDirectoryName(filePath) ?? string.Empty;
    }

    /// <summary>
    /// Combines path segments into a full path.
    /// </summary>
    public string CombinePaths(params string[] paths)
    {
        return _fileSystem.Path.Combine(paths);
    }

    private static bool IsProjectFile(string extension)
    {
        return ProjectFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsSourceFile(string extension)
    {
        return SourceFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    private static string ExtractTargetFramework(XDocument document)
    {
        // Try to find TargetFramework element
        var targetFramework = document.Descendants("TargetFramework").FirstOrDefault()?.Value;
        if (!string.IsNullOrWhiteSpace(targetFramework))
            return targetFramework;

        // Try to find TargetFrameworks element (multi-targeting)
        var targetFrameworks = document.Descendants("TargetFrameworks").FirstOrDefault()?.Value;
        if (!string.IsNullOrWhiteSpace(targetFrameworks))
        {
            // Return the first target framework for simplicity
            return targetFrameworks
                    .Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault() ?? "net9.0";
        }

        // Default to net9.0 if not found
        return "net9.0";
    }
}
