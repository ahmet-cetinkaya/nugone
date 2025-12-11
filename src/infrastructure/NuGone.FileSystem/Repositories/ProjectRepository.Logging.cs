using Microsoft.Extensions.Logging;

namespace NuGone.FileSystem.Repositories;

/// <summary>
/// High-performance logging methods for ProjectRepository.
/// Uses source generator to avoid expensive argument evaluation when logging is disabled.
/// </summary>
public partial class ProjectRepository
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Discovering project files in: {RootPath}")]
    private partial void LogDiscoveringProjectFiles(string rootPath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Root path does not exist: {RootPath}")]
    private partial void LogRootPathNotExists(string rootPath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Found project file: {ProjectFile}")]
    private partial void LogFoundProjectFile(string projectFile);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Error discovering project files in: {RootPath}"
    )]
    private partial void LogErrorDiscoveringProjectFiles(Exception ex, string rootPath);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Discovered {Count} project file(s) in: {RootPath}"
    )]
    private partial void LogDiscoveredProjectFiles(int count, string rootPath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Loading project: {ProjectFilePath}")]
    private partial void LogLoadingProject(string projectFilePath);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Loaded project: {ProjectName} ({TargetFramework}) with {GlobalUsingCount} global usings"
    )]
    private partial void LogLoadedProject(
        string projectName,
        string targetFramework,
        int globalUsingCount
    );

    [LoggerMessage(Level = LogLevel.Error, Message = "Error loading project: {ProjectFilePath}")]
    private partial void LogErrorLoadingProject(Exception ex, string projectFilePath);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Getting source files for project: {ProjectName}"
    )]
    private partial void LogGettingSourceFiles(string projectName);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Project directory does not exist: {ProjectDirectory}"
    )]
    private partial void LogProjectDirectoryNotExists(string projectDirectory);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Error getting source files for project: {ProjectName}"
    )]
    private partial void LogErrorGettingSourceFiles(Exception ex, string projectName);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Found {Count} source file(s) for project: {ProjectName}"
    )]
    private partial void LogFoundSourceFiles(int count, string projectName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error reading file: {FilePath}")]
    private partial void LogErrorReadingFile(Exception ex, string filePath);
}
