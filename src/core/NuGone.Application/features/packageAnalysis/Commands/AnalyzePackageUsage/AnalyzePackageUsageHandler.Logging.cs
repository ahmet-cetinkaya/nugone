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
    private static partial void LogStartingAnalysis(ILogger logger, string path);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Package usage analysis completed in {ElapsedTime}"
    )]
    private static partial void LogAnalysisCompleted(ILogger logger, TimeSpan elapsedTime);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error loading solution: {SolutionPath}")]
    private static partial void LogErrorLoadingSolution(
        ILogger logger,
        Exception ex,
        string solutionPath
    );

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Error loading project as solution: {ProjectPath}"
    )]
    private static partial void LogErrorLoadingProjectAsSolution(
        ILogger logger,
        Exception ex,
        string projectPath
    );

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Error loading directory as solution: {DirectoryPath}"
    )]
    private static partial void LogErrorLoadingDirectoryAsSolution(
        ILogger logger,
        Exception ex,
        string directoryPath
    );
}
