using Microsoft.Extensions.Logging;

namespace NuGone.Application.Features.PackageAnalysis.Services;

/// <summary>
/// High-performance logging methods for PackageUsageAnalyzer.
/// Uses source generator to avoid expensive argument evaluation when logging is disabled.
/// </summary>
public partial class PackageUsageAnalyzer
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Starting package usage analysis for solution: {SolutionName}"
    )]
    private partial void LogStartingAnalysis(string solutionName);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Completed package usage analysis for solution: {SolutionName}"
    )]
    private partial void LogCompletedAnalysis(string solutionName);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Analyzing package usage for project: {ProjectName}"
    )]
    private partial void LogAnalyzingProject(string projectName);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Analyzing package: {PackageId} in project: {ProjectName}"
    )]
    private partial void LogAnalyzingPackage(string packageId, string projectName);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Package {PackageId} has {NamespaceCount} namespaces: {Namespaces}"
    )]
    private partial void LogPackageNamespaces(
        string packageId,
        int namespaceCount,
        string namespaces
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No namespaces found for package: {PackageId}"
    )]
    private partial void LogNoNamespacesFound(string packageId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Package {PackageId} usage scan found {UsageCount} namespace matches"
    )]
    private partial void LogPackageUsageScan(string packageId, int usageCount);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Package {PackageId} analysis result: IsUsed={IsUsed}, UsageLocations={LocationCount}"
    )]
    private partial void LogPackageAnalysisResult(string packageId, bool isUsed, int locationCount);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Project {ProjectName}: {UsedCount} used, {UnusedCount} unused packages"
    )]
    private partial void LogProjectSummary(string projectName, int usedCount, int unusedCount);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Error scanning file for namespace usage: {SourceFile}"
    )]
    private partial void LogErrorScanningFile(Exception ex, string sourceFile);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Error scanning file for global namespace usage: {SourceFile}"
    )]
    private partial void LogErrorScanningGlobalUsage(Exception ex, string sourceFile);
}
