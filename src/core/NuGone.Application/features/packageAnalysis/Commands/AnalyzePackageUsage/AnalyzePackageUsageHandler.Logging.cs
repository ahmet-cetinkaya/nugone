using Microsoft.Extensions.Logging;

namespace NuGone.Application.Features.PackageAnalysis.Commands.AnalyzePackageUsage;

/// <summary>
/// High-performance logging methods for AnalyzePackageUsageHandler.
/// Uses source generator to avoid expensive argument evaluation when logging is disabled.
/// </summary>
public partial class AnalyzePackageUsageHandler
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Starting package usage analysis for path: {Path}"
    )]
    private partial void LogStartingAnalysis(string path);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Package usage analysis completed in {ElapsedTime}"
    )]
    private partial void LogAnalysisCompleted(TimeSpan elapsedTime);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error loading solution: {SolutionPath}")]
    private partial void LogErrorLoadingSolution(Exception ex, string solutionPath);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Error loading project as solution: {ProjectPath}"
    )]
    private partial void LogErrorLoadingProjectAsSolution(Exception ex, string projectPath);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Error loading directory as solution: {DirectoryPath}"
    )]
    private partial void LogErrorLoadingDirectoryAsSolution(Exception ex, string directoryPath);
}
