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
public partial class ProjectRepository(
    IFileSystem fileSystem,
    INuGetRepository nugetRepository,
    ILogger<ProjectRepository> logger
) : IProjectRepository
{
    private readonly IFileSystem _fileSystem =
        fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    private readonly INuGetRepository _nugetRepository =
        nugetRepository ?? throw new ArgumentNullException(nameof(nugetRepository));
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
        LogDiscoveringProjectFiles(rootPath);

        if (!await ExistsAsync(rootPath))
        {
            LogRootPathNotExists(rootPath);
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
                    LogFoundProjectFile(file.FullName);
                }
            }
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            LogErrorDiscoveringProjectFiles(ex, rootPath);
            throw;
        }

        LogDiscoveredProjectFiles(projectFiles.Count, rootPath);
        return projectFiles;
    }

    /// <summary>
    /// Loads project information from a project file.
    /// RFC-0002: Project file parsing for metadata extraction.
    /// Enhanced to support global Using declarations.
    /// </summary>
    public async Task<Project> LoadProjectAsync(
        string projectFilePath,
        CancellationToken cancellationToken = default
    )
    {
        LogLoadingProject(projectFilePath);

        if (!await ExistsAsync(projectFilePath))
            throw new FileNotFoundException($"Project file not found: {projectFilePath}");

        try
        {
            var content = await ReadSourceFileAsync(projectFilePath, cancellationToken);
            var document = XDocument.Parse(content);

            var projectName = _fileSystem.Path.GetFileNameWithoutExtension(projectFilePath);
            var targetFramework = ExtractTargetFramework(document);

            var project = new Project(projectFilePath, projectName, targetFramework);

            // Load global using declarations
            var globalUsings = await _nugetRepository.ExtractGlobalUsingsAsync(
                projectFilePath,
                cancellationToken
            );
            foreach (var globalUsing in globalUsings)
            {
                project.AddGlobalUsing(globalUsing);
            }

            // Add default exclude patterns
            project.AddExcludePattern("**/bin/**");
            project.AddExcludePattern("**/obj/**");
            project.AddExcludePattern("**/.vs/**");
            project.AddExcludePattern("**/.git/**");

            LogLoadedProject(projectName, targetFramework, project.GlobalUsings.Count);
            return project;
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            LogErrorLoadingProject(ex, projectFilePath);
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
        LogGettingSourceFiles(project.Name);

        var projectDirectory = GetDirectoryPath(project.FilePath);
        if (!await ExistsAsync(projectDirectory))
        {
            LogProjectDirectoryNotExists(projectDirectory);
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
            LogErrorGettingSourceFiles(ex, project.Name);
            throw;
        }

        LogFoundSourceFiles(sourceFiles.Count, project.Name);
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
            LogErrorReadingFile(ex, filePath);
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
                    .FirstOrDefault()
                ?? "net9.0";
        }

        // Default to net9.0 if not found
        return "net9.0";
    }
}
