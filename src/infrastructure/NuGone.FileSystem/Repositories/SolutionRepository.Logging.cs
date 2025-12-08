using Microsoft.Extensions.Logging;

namespace NuGone.FileSystem.Repositories;

/// <summary>
/// High-performance logging methods for SolutionRepository.
/// Uses source generator to avoid expensive argument evaluation when logging is disabled.
/// </summary>
public partial class SolutionRepository
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Discovering solution files in: {RootPath}")]
    private partial void LogDiscoveringSolutionFiles(string rootPath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Root path does not exist: {RootPath}")]
    private partial void LogRootPathNotExists(string rootPath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Found solution file: {SolutionFile}")]
    private partial void LogFoundSolutionFile(string solutionFile);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Error discovering solution files in: {RootPath}"
    )]
    private partial void LogErrorDiscoveringSolutionFiles(Exception ex, string rootPath);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Discovered {Count} solution file(s) in: {RootPath}"
    )]
    private partial void LogDiscoveredSolutionFiles(int count, string rootPath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Loading solution: {SolutionFilePath}")]
    private partial void LogLoadingSolution(string solutionFilePath);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Loaded solution: {SolutionName} with {ProjectCount} project(s)"
    )]
    private partial void LogLoadedSolution(string solutionName, int projectCount);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error loading solution: {SolutionFilePath}")]
    private partial void LogErrorLoadingSolution(Exception ex, string solutionFilePath);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Error checking central package management in: {DirectoryPackagesPropsPath}"
    )]
    private partial void LogErrorCheckingCpm(Exception ex, string directoryPackagesPropsPath);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Loading central package versions from: {DirectoryPackagesPropsPath}"
    )]
    private partial void LogLoadingCentralPackageVersions(string directoryPackagesPropsPath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Loaded {Count} central package version(s)")]
    private partial void LogLoadedCentralPackageVersions(int count);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Error loading central package versions from: {DirectoryPackagesPropsPath}"
    )]
    private partial void LogErrorLoadingCentralPackageVersions(
        Exception ex,
        string directoryPackagesPropsPath
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Project entry missing Path element or attribute in: {SolutionFilePath}"
    )]
    private partial void LogMissingProjectPath(string solutionFilePath);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Added project: {ProjectName} from {RelativePath}"
    )]
    private partial void LogAddedProject(string projectName, string relativePath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Project file not found: {ProjectPath}")]
    private partial void LogProjectFileNotFound(string projectPath);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Invalid XML format in .slnx file: {SolutionFilePath}"
    )]
    private partial void LogInvalidSlnxFormat(Exception ex, string solutionFilePath);
}
