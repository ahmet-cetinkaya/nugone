using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using NuGone.Application.Features.PackageAnalysis.Services.Abstractions;
using NuGone.Domain.Features.PackageAnalysis.Entities;

namespace NuGone.NuGet.Repositories;

/// <summary>
/// NuGet implementation of the NuGet repository.
/// Handles package reference extraction and metadata operations as specified in RFC-0002.
/// </summary>
public partial class NuGetRepository(ILogger<NuGetRepository> logger) : INuGetRepository
{
    private readonly ILogger<NuGetRepository> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    // Common development dependency package patterns
    private static readonly HashSet<string> DevelopmentDependencyPatterns = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        "Microsoft.CodeAnalysis",
        "Microsoft.CodeAnalysis.Analyzers",
        "Microsoft.CodeAnalysis.CSharp",
        "Microsoft.CodeAnalysis.VisualBasic",
        "Microsoft.NET.Test.Sdk",
        "xunit",
        "xunit.runner",
        "NUnit",
        "MSTest",
        "Moq",
        "FluentAssertions",
        "coverlet",
        "ReportGenerator",
        "Swashbuckle",
        "StyleCop",
        "SonarAnalyzer",
    };

    /// <summary>
    /// Extracts package references from a project file.
    /// RFC-0002: Package reference discovery from project files.
    /// Enhanced to support global Using declarations.
    /// </summary>
    public async Task<IEnumerable<PackageReference>> ExtractPackageReferencesAsync(
        string projectFilePath,
        Dictionary<string, string>? centralPackageVersions = null,
        CancellationToken cancellationToken = default
    )
    {
        LogExtractingPackageReferences(projectFilePath);

        if (!File.Exists(projectFilePath))
            throw new FileNotFoundException($"Project file not found: {projectFilePath}");

        try
        {
            var content = await File.ReadAllTextAsync(projectFilePath, cancellationToken)
                .ConfigureAwait(false);
            var document = XDocument.Parse(content);

            var packageReferences = new List<PackageReference>();
            var globalUsings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // First, collect all global using declarations
            foreach (var usingElement in document.Descendants("Using"))
            {
                var include = usingElement.Attribute("Include")?.Value;
                if (!string.IsNullOrWhiteSpace(include))
                {
                    _ = globalUsings.Add(include);
                }
            }

            // Then, extract package references and mark those with global usings
            foreach (var packageRefElement in document.Descendants("PackageReference"))
            {
                var include = packageRefElement.Attribute("Include")?.Value;
                if (string.IsNullOrWhiteSpace(include))
                    continue;

                var version = GetPackageVersion(packageRefElement);
                if (string.IsNullOrWhiteSpace(version))
                {
                    // Try to resolve from central package versions
                    if (
                        centralPackageVersions != null
                        && centralPackageVersions.TryGetValue(include, out var centralVersion)
                    )
                    {
                        version = centralVersion;
                    }
                    else
                    {
                        LogNoVersionFound(include, projectFilePath);
                        continue;
                    }
                }

                var condition = packageRefElement.Attribute("Condition")?.Value;
                var hasGlobalUsing = globalUsings.Contains(include);

                var packageRef = new PackageReference(
                    include,
                    version,
                    projectFilePath,
                    true,
                    condition,
                    hasGlobalUsing
                );
                packageReferences.Add(packageRef);
            }

            var globalUsingCount = packageReferences.Count(p => p.HasGlobalUsing);
            LogExtractedPackageReferences(
                packageReferences.Count,
                projectFilePath,
                globalUsingCount
            );
            return packageReferences;
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            LogErrorExtractingPackageReferences(ex, projectFilePath);
            throw;
        }
    }

    /// <summary>
    /// Gets the namespaces provided by a specific NuGet package.
    /// RFC-0002: Namespace discovery for usage scanning.
    /// </summary>
    public async Task<IEnumerable<string>> GetPackageNamespacesAsync(
        string packageId,
        string version,
        string targetFramework,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(packageId);
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(targetFramework);

        LogGettingNamespaces(packageId, version, targetFramework);

        // For now, return common namespace patterns based on package ID
        // In a full implementation, this would analyze the actual package assemblies
        var namespaces = GetCommonNamespacesForPackage(packageId);

        await Task.CompletedTask.ConfigureAwait(false); // Placeholder for async operations
        return namespaces;
    }

    /// <summary>
    /// Gets metadata for a specific NuGet package.
    /// RFC-0002: Package metadata for enhanced analysis.
    /// </summary>
    public async Task<PackageMetadata?> GetPackageMetadataAsync(
        string packageId,
        string version,
        CancellationToken cancellationToken = default
    )
    {
        LogGettingMetadata(packageId, version);

        // For now, return basic metadata based on known patterns
        // In a full implementation, this would query NuGet API or local cache
        var isDevelopmentDependency = IsDevelopmentDependency(packageId);
        var metadata = new PackageMetadata(
            packageId,
            version,
            isDevelopmentDependency: isDevelopmentDependency
        );

        await Task.CompletedTask.ConfigureAwait(false); // Placeholder for async operations
        return metadata;
    }

    /// <summary>
    /// Resolves transitive dependencies for a package.
    /// RFC-0002: Transitive dependency analysis.
    /// </summary>
    public async Task<IEnumerable<PackageReference>> ResolveTransitiveDependenciesAsync(
        string packageId,
        string version,
        string targetFramework,
        CancellationToken cancellationToken = default
    )
    {
        LogResolvingTransitiveDependencies(packageId, version, targetFramework);

        // For now, return empty collection
        // In a full implementation, this would analyze package dependencies
        await Task.CompletedTask.ConfigureAwait(false); // Placeholder for async operations
        return Enumerable.Empty<PackageReference>();
    }

    /// <summary>
    /// Checks if a package is a development dependency (tools, analyzers, etc.).
    /// RFC-0002: Development dependency identification.
    /// </summary>
    public bool IsDevelopmentDependency(string packageId, PackageMetadata? packageMetadata = null)
    {
        if (packageMetadata?.IsDevelopmentDependency == true)
            return true;

        // Check against known development dependency patterns
        return DevelopmentDependencyPatterns.Any(pattern =>
            packageId.Contains(pattern, StringComparison.OrdinalIgnoreCase)
        );
    }

    /// <summary>
    /// Extracts global using declarations from a project file.
    /// Global usings make package namespaces available throughout the project without explicit using statements.
    /// </summary>
    public async Task<IEnumerable<GlobalUsing>> ExtractGlobalUsingsAsync(
        string projectFilePath,
        CancellationToken cancellationToken = default
    )
    {
        LogExtractingGlobalUsings(projectFilePath);

        if (!File.Exists(projectFilePath))
            throw new FileNotFoundException($"Project file not found: {projectFilePath}");

        try
        {
            var content = await File.ReadAllTextAsync(projectFilePath, cancellationToken)
                .ConfigureAwait(false);
            var document = XDocument.Parse(content);

            var globalUsings = new List<GlobalUsing>();

            foreach (var usingElement in document.Descendants("Using"))
            {
                var include = usingElement.Attribute("Include")?.Value;
                if (string.IsNullOrWhiteSpace(include))
                    continue;

                var condition = usingElement.Attribute("Condition")?.Value;
                var globalUsing = new GlobalUsing(include, projectFilePath, condition);
                globalUsings.Add(globalUsing);
            }

            LogExtractedGlobalUsings(globalUsings.Count, projectFilePath);
            return globalUsings;
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            LogErrorExtractingGlobalUsings(ex, projectFilePath);
            throw;
        }
    }

    private static string? GetPackageVersion(XElement packageRefElement)
    {
        // Get version from the element itself
        var version = packageRefElement.Attribute("Version")?.Value;
        return !string.IsNullOrWhiteSpace(version) ? version : null;
    }

    private static IEnumerable<string> GetCommonNamespacesForPackage(string packageId)
    {
        // Return common namespace patterns based on package ID
        // This is a simplified implementation - a full version would analyze actual assemblies
        var namespaces = new List<string>();

        // Add the package ID as a potential namespace
        namespaces.Add(packageId);

        // Add common variations
        if (packageId.Contains('.', StringComparison.Ordinal))
        {
            var parts = packageId.Split('.');
            if (parts.Length >= 2)
            {
                // Add root namespace (e.g., "Microsoft" from "Microsoft.Extensions.Logging")
                namespaces.Add(parts[0]);

                // Add intermediate namespaces
                for (int i = 1; i < parts.Length; i++)
                {
                    var @namespace = string.Join(".", parts.Take(i + 1));
                    namespaces.Add(@namespace);
                }
            }
        }

        // Add some common patterns for well-known packages
        switch (packageId.ToUpperInvariant())
        {
            case "NEWTONSOFT.JSON":
                namespaces.AddRange(["Newtonsoft.Json", "Newtonsoft.Json.Linq"]);
                break;
            case "SYSTEM.TEXT.JSON":
                namespaces.AddRange(["System.Text.Json", "System.Text.Json.Serialization"]);
                break;
            case "MICROSOFT.EXTENSIONS.LOGGING":
                namespaces.AddRange([
                    "Microsoft.Extensions.Logging",
                    "Microsoft.Extensions.DependencyInjection",
                ]);
                break;
            case "SPECTRE.CONSOLE":
                namespaces.AddRange(["Spectre.Console", "Spectre.Console.Cli"]);
                break;
            case "XUNIT":
                namespaces.AddRange(["Xunit", "Xunit.Abstractions"]);
                break;
            case "MOQ":
                namespaces.Add("Moq");
                break;
            case "FLUENTASSERTIONS":
                namespaces.Add("FluentAssertions");
                break;
        }

        return namespaces.Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
