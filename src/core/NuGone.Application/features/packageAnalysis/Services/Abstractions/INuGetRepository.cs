using NuGone.Domain.Features.PackageAnalysis.Entities;

namespace NuGone.Application.Features.PackageAnalysis.Services.Abstractions;

/// <summary>
/// Repository interface for NuGet package-related operations.
/// Defines the contract for accessing package metadata as specified in RFC-0002.
/// </summary>
public interface INuGetRepository
{
    /// <summary>
    /// Extracts package references from a project file.
    /// RFC-0002: Package reference discovery from project files.
    /// </summary>
    /// <param name="projectFilePath">Path to the project file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of package references</returns>
    Task<IEnumerable<PackageReference>> ExtractPackageReferencesAsync(
        string projectFilePath,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets the namespaces provided by a specific NuGet package.
    /// RFC-0002: Namespace discovery for usage scanning.
    /// </summary>
    /// <param name="packageId">The package identifier</param>
    /// <param name="version">The package version</param>
    /// <param name="targetFramework">The target framework</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of namespaces</returns>
    Task<IEnumerable<string>> GetPackageNamespacesAsync(
        string packageId,
        string version,
        string targetFramework,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets metadata for a specific NuGet package.
    /// RFC-0002: Package metadata for enhanced analysis.
    /// </summary>
    /// <param name="packageId">The package identifier</param>
    /// <param name="version">The package version</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Package metadata</returns>
    Task<PackageMetadata?> GetPackageMetadataAsync(
        string packageId,
        string version,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Resolves transitive dependencies for a package.
    /// RFC-0002: Transitive dependency analysis.
    /// </summary>
    /// <param name="packageId">The package identifier</param>
    /// <param name="version">The package version</param>
    /// <param name="targetFramework">The target framework</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of transitive package references</returns>
    Task<IEnumerable<PackageReference>> ResolveTransitiveDependenciesAsync(
        string packageId,
        string version,
        string targetFramework,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Checks if a package is a development dependency (tools, analyzers, etc.).
    /// RFC-0002: Development dependency identification.
    /// </summary>
    /// <param name="packageId">The package identifier</param>
    /// <param name="packageMetadata">Optional package metadata</param>
    /// <returns>True if the package is a development dependency</returns>
    bool IsDevelopmentDependency(string packageId, PackageMetadata? packageMetadata = null);
}

/// <summary>
/// Represents metadata for a NuGet package.
/// </summary>
public class PackageMetadata
{
    public PackageMetadata(
        string id,
        string version,
        string? description = null,
        IEnumerable<string>? tags = null,
        bool isDevelopmentDependency = false
    )
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Description = description;
        Tags = tags?.ToList() ?? new List<string>();
        IsDevelopmentDependency = isDevelopmentDependency;
    }

    /// <summary>
    /// The package identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// The package version.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// The package description.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Tags associated with the package.
    /// </summary>
    public IList<string> Tags { get; }

    /// <summary>
    /// Indicates whether this is a development dependency.
    /// </summary>
    public bool IsDevelopmentDependency { get; }
}
