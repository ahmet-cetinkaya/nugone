using Microsoft.Extensions.Logging;

namespace NuGone.NuGet.Repositories;

/// <summary>
/// High-performance logging methods for NuGetRepository.
/// Uses source generator to avoid expensive argument evaluation when logging is disabled.
/// </summary>
public partial class NuGetRepository
{
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Extracting package references from: {ProjectFilePath}"
    )]
    private partial void LogExtractingPackageReferences(string projectFilePath);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No version found for package: {PackageId} in project: {ProjectFilePath}"
    )]
    private partial void LogNoVersionFound(string packageId, string projectFilePath);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Extracted {Count} package reference(s) from: {ProjectFilePath}, {GlobalUsingCount} with global usings"
    )]
    private partial void LogExtractedPackageReferences(
        int count,
        string projectFilePath,
        int globalUsingCount
    );

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Error extracting package references from: {ProjectFilePath}"
    )]
    private partial void LogErrorExtractingPackageReferences(Exception ex, string projectFilePath);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Getting namespaces for package: {PackageId} {Version} ({TargetFramework})"
    )]
    private partial void LogGettingNamespaces(
        string packageId,
        string version,
        string targetFramework
    );

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Getting metadata for package: {PackageId} {Version}"
    )]
    private partial void LogGettingMetadata(string packageId, string version);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Resolving transitive dependencies for: {PackageId} {Version} ({TargetFramework})"
    )]
    private partial void LogResolvingTransitiveDependencies(
        string packageId,
        string version,
        string targetFramework
    );

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Extracting global usings from: {ProjectFilePath}"
    )]
    private partial void LogExtractingGlobalUsings(string projectFilePath);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Extracted {Count} global using(s) from: {ProjectFilePath}"
    )]
    private partial void LogExtractedGlobalUsings(int count, string projectFilePath);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Error extracting global usings from: {ProjectFilePath}"
    )]
    private partial void LogErrorExtractingGlobalUsings(Exception ex, string projectFilePath);
}
