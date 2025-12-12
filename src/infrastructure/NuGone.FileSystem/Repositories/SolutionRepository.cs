using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using NuGone.Application.Features.PackageAnalysis.Services.Abstractions;
using NuGone.Domain.Features.PackageAnalysis.Entities;

namespace NuGone.FileSystem.Repositories;

/// <summary>
/// File system implementation of the solution repository.
/// Handles solution file discovery and parsing as specified in RFC-0002.
/// </summary>
public partial class SolutionRepository(IFileSystem fileSystem, ILogger<SolutionRepository> logger)
    : ISolutionRepository
{
    private readonly IFileSystem _fileSystem =
        fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    private readonly ILogger<SolutionRepository> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private static readonly string[] SolutionFileExtensions = [".sln", ".slnx"];
    private static readonly Regex ProjectLineRegex = MyRegex();

    /// <summary>
    /// Discovers solution files in a given directory.
    /// RFC-0002: Solution discovery for analysis.
    /// </summary>
    public Task<IEnumerable<string>> DiscoverSolutionFilesAsync(
        string rootPath,
        CancellationToken cancellationToken = default
    )
    {
        LogDiscoveringSolutionFiles(rootPath);

        if (!_fileSystem.Directory.Exists(rootPath))
        {
            LogRootPathNotExists(rootPath);
            return Task.FromResult(Enumerable.Empty<string>());
        }

        var solutionFiles = new List<string>();

        try
        {
            var directoryInfo = _fileSystem.DirectoryInfo.New(rootPath);
            var allFiles = directoryInfo.GetFiles("*", SearchOption.TopDirectoryOnly);

            foreach (var file in allFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (IsSolutionFile(file.Extension))
                {
                    solutionFiles.Add(file.FullName);
                    LogFoundSolutionFile(file.FullName);
                }
            }
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            LogErrorDiscoveringSolutionFiles(ex, rootPath);
            throw;
        }

        LogDiscoveredSolutionFiles(solutionFiles.Count, rootPath);
        return Task.FromResult<IEnumerable<string>>(solutionFiles);
    }

    /// <summary>
    /// Loads solution information from a solution file.
    /// RFC-0002: Solution file parsing for metadata extraction.
    /// </summary>
    public async Task<Solution> LoadSolutionAsync(
        string solutionFilePath,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(solutionFilePath);

        LogLoadingSolution(solutionFilePath);

        if (!_fileSystem.File.Exists(solutionFilePath))
            throw new FileNotFoundException($"Solution file not found: {solutionFilePath}");

        try
        {
            // Normalize path separators for cross-platform compatibility
            var normalizedSolutionPath = solutionFilePath.Replace(
                '\\',
                Path.DirectorySeparatorChar
            );
            var solutionName = Path.GetFileNameWithoutExtension(normalizedSolutionPath);
            var solution = new Solution(solutionFilePath, solutionName);

            // Check for central package management - fix for MockFileSystem GetDirectoryName issue
            var solutionDirectory = _fileSystem.Path.GetDirectoryName(solutionFilePath);
            if (string.IsNullOrEmpty(solutionDirectory))
            {
                // Extract directory manually if GetDirectoryName fails
                var lastSeparator = Math.Max(
                    solutionFilePath.LastIndexOf('\\'),
                    solutionFilePath.LastIndexOf('/')
                );
                solutionDirectory =
                    lastSeparator > 0 ? solutionFilePath.Substring(0, lastSeparator) : string.Empty;
            }
            // Parse project references based on solution file type relative to the solution
            // We parse projects first so we can use their locations to find Directory.Packages.props
            if (solutionFilePath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
            {
                await ParseSlnxFileAsync(solution, solutionFilePath, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await ParseSlnFileAsync(solution, solutionFilePath, cancellationToken)
                    .ConfigureAwait(false);
            }

            var (isEnabled, directoryPackagesPropsPath) = await CheckCentralPackageManagementAsync(
                    solutionDirectory,
                    cancellationToken
                )
                .ConfigureAwait(false);

            // If not found at solution level, check project levels
            if (!isEnabled && solution.Projects.Count > 0)
            {
                var projectCpmRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var project in solution.Projects)
                {
                    var (isProjectCpmEnabled, projectCpmPath) =
                        await CheckCentralPackageManagementAsync(
                                project.DirectoryPath,
                                cancellationToken
                            )
                            .ConfigureAwait(false);

                    if (isProjectCpmEnabled && !string.IsNullOrWhiteSpace(projectCpmPath))
                    {
                        projectCpmRoots.Add(projectCpmPath);
                        LogProjectCpmDetected(project.DirectoryPath, projectCpmPath);
                    }
                }

                if (projectCpmRoots.Count == 1)
                {
                    // All CPM-enabled projects share the same root - treat it as the solution root
                    isEnabled = true;
                    directoryPackagesPropsPath = projectCpmRoots.Single();
                }
                else if (projectCpmRoots.Count > 1)
                {
                    // Multiple distinct CPM roots discovered
                    // Choose deterministically by:
                    //  1. Path length (shortest is considered "closest" to the solution root)
                    //     This typically represents the most common/root-level CPM configuration
                    //  2. Lexicographical order as a stable tiebreaker for reproducibility
                    //
                    // Note: This heuristic works well for most scenarios where projects have
                    // inherited CPM from parent directories, but may not match complex
                    // custom MSBuild import hierarchies.
                    var orderedRoots = projectCpmRoots
                        .OrderBy(path => path.Length)
                        .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    isEnabled = true;
                    directoryPackagesPropsPath = orderedRoots.First();

                    // Log to inform users about the CPM root selection
                    LogMultipleCpmRootsDetected(projectCpmRoots.Count, directoryPackagesPropsPath);
                }
            }

            if (isEnabled && !string.IsNullOrEmpty(directoryPackagesPropsPath))
            {
                solution.EnableCentralPackageManagement(directoryPackagesPropsPath);
            }

            LogLoadedSolution(solutionName, solution.Projects.Count);
            return solution;
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            LogErrorLoadingSolution(ex, solutionFilePath);
            throw;
        }
    }

    /// <summary>
    /// Checks if central package management is enabled by searching up from a given directory.
    /// RFC-0002: Central package management detection.
    /// </summary>
    /// <param name="startDirectoryPath">Directory to start searching from (can be solution or project directory)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result tuple indicating if enabled and path to props file</returns>
    public async Task<(
        bool IsEnabled,
        string? DirectoryPackagesPropsPath
    )> CheckCentralPackageManagementAsync(
        string startDirectoryPath,
        CancellationToken cancellationToken = default
    )
    {
        var currentDirectory = startDirectoryPath;
        string? directoryPackagesPropsPath = null;

        while (!string.IsNullOrEmpty(currentDirectory))
        {
            var path = _fileSystem.Path.Combine(currentDirectory, "Directory.Packages.props");
            if (_fileSystem.File.Exists(path))
            {
                directoryPackagesPropsPath = path;
                break;
            }

            // Fix for MockFileSystem GetDirectoryName issue on Unix with Windows paths
            var parentDir = _fileSystem.Path.GetDirectoryName(currentDirectory);
            if (
                string.IsNullOrEmpty(parentDir)
                && (
                    currentDirectory.Contains('/', StringComparison.Ordinal)
                    || currentDirectory.Contains('\\', StringComparison.Ordinal)
                )
            )
            {
                var lastSeparator = Math.Max(
                    currentDirectory.LastIndexOf('\\'),
                    currentDirectory.LastIndexOf('/')
                );

                // Special handling for Windows drive-rooted paths (e.g., "C:\foo")
                if (lastSeparator > 0)
                {
                    // Check if this is a Windows drive path (e.g., "C:\foo")
                    if (
                        lastSeparator == 2
                        && currentDirectory.Length > 2
                        && currentDirectory[1] == ':'
                    )
                    {
                        // For "C:\foo", parent should be "C:\"
                        parentDir = currentDirectory.Substring(0, lastSeparator + 1); // Include the backslash
                    }
                    else
                    {
                        parentDir = currentDirectory.Substring(0, lastSeparator);
                    }
                }
                else
                {
                    parentDir = null;
                }
            }

            // To prevent infinite loops if GetDirectoryName returns same path or empty
            if (string.IsNullOrEmpty(parentDir) || parentDir == currentDirectory)
            {
                break;
            }
            currentDirectory = parentDir;
        }

        if (directoryPackagesPropsPath == null)
            return (false, null);

        try
        {
            var isEnabled = await IsCpmEnabledRecursiveAsync(
                    directoryPackagesPropsPath,
                    new HashSet<string>(StringComparer.Ordinal),
                    cancellationToken
                )
                .ConfigureAwait(false);

            return (isEnabled, isEnabled ? directoryPackagesPropsPath : null);
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            LogErrorCheckingCpm(ex, directoryPackagesPropsPath);
            return (false, null);
        }
    }

    private async Task<bool> IsCpmEnabledRecursiveAsync(
        string path,
        HashSet<string> visitedPaths,
        CancellationToken cancellationToken
    )
    {
        if (!visitedPaths.Add(path))
            return false; // Cycle detection

        if (!_fileSystem.File.Exists(path))
            return false;

        var content = await _fileSystem
            .File.ReadAllTextAsync(path, cancellationToken)
            .ConfigureAwait(false);
        var document = XDocument.Parse(content);

        // Check locally for 'true' first - this takes precedence
        if (
            document
                .Descendants("ManagePackageVersionsCentrally")
                .Any(e => e.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
        )
        {
            return true;
        }

        // Check imports recursively
        foreach (var import in document.Descendants("Import"))
        {
            var projectAttribute = import.Attribute("Project")?.Value;
            if (!string.IsNullOrWhiteSpace(projectAttribute))
            {
                var directory = _fileSystem.Path.GetDirectoryName(path);
                var importedPath = ResolveProjectPath(directory ?? string.Empty, projectAttribute);

                if (
                    await IsCpmEnabledRecursiveAsync(importedPath, visitedPaths, cancellationToken)
                        .ConfigureAwait(false)
                )
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Loads central package versions from Directory.Packages.props.
    /// RFC-0002: Central package version resolution.
    /// </summary>
    public async Task<Dictionary<string, string>> LoadCentralPackageVersionsAsync(
        string directoryPackagesPropsPath,
        CancellationToken cancellationToken = default
    )
    {
        LogLoadingCentralPackageVersions(directoryPackagesPropsPath);

        if (!_fileSystem.File.Exists(directoryPackagesPropsPath))
            throw new FileNotFoundException(
                $"Directory.Packages.props not found: {directoryPackagesPropsPath}"
            );

        try
        {
            var packageVersions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            await LoadCentralPackageVersionsRecursiveAsync(
                    directoryPackagesPropsPath,
                    packageVersions,
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    cancellationToken
                )
                .ConfigureAwait(false);

            LogLoadedCentralPackageVersions(packageVersions.Count);
            return packageVersions;
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            LogErrorLoadingCentralPackageVersions(ex, directoryPackagesPropsPath);
            throw;
        }
    }

    private async Task LoadCentralPackageVersionsRecursiveAsync(
        string path,
        Dictionary<string, string> packageVersions,
        HashSet<string> visitedPaths,
        CancellationToken cancellationToken
    )
    {
        if (!visitedPaths.Add(path))
            return; // Cycle detection

        if (!_fileSystem.File.Exists(path))
            return;

        var content = await _fileSystem
            .File.ReadAllTextAsync(path, cancellationToken)
            .ConfigureAwait(false);
        var document = XDocument.Parse(content);

        // Current implementation does not strictly follow MSBuild evaluation order (which would handle imports interleaved with items).
        // Instead, it loads Imports first (base values), then overrides with local values. This is generally correct for property composition.

        // 1. Process Imports first (Base values)
        foreach (var import in document.Descendants("Import"))
        {
            var projectAttribute = import.Attribute("Project")?.Value;
            if (!string.IsNullOrWhiteSpace(projectAttribute))
            {
                var directory = _fileSystem.Path.GetDirectoryName(path);
                var importedPath = ResolveProjectPath(directory ?? string.Empty, projectAttribute);
                await LoadCentralPackageVersionsRecursiveAsync(
                        importedPath,
                        packageVersions,
                        visitedPaths,
                        cancellationToken
                    )
                    .ConfigureAwait(false);
            }
        }

        // 2. Process Local PackageVersions (Overrides)
        foreach (var packageVersionElement in document.Descendants("PackageVersion"))
        {
            var include =
                packageVersionElement.Attribute("Include")?.Value
                ?? packageVersionElement.Attribute("Update")?.Value;
            var version = packageVersionElement.Attribute("Version")?.Value;

            if (!string.IsNullOrWhiteSpace(include) && !string.IsNullOrWhiteSpace(version))
            {
                packageVersions[include] = version;
            }
        }
    }

    /// <summary>
    /// Resolves the full path to a project file relative to the solution directory.
    /// Handles cross-platform path compatibility for both real and mock file systems.
    /// Supports relative paths with .. and . navigation.
    /// </summary>
    public string ResolveProjectPath(string solutionDirectoryPath, string relativeProjectPath)
    {
        ArgumentNullException.ThrowIfNull(solutionDirectoryPath);
        ArgumentNullException.ThrowIfNull(relativeProjectPath);

        // Use Path.Combine to handle relative paths properly
        var fullPath = _fileSystem.Path.Combine(solutionDirectoryPath, relativeProjectPath);

        // For MockFileSystem compatibility on Unix systems with Windows paths
        // Try different path formats if the initial one doesn't exist
        if (!_fileSystem.File.Exists(fullPath))
        {
            // Try with leading slash (MockFileSystem format)
            var withLeadingSlash = "/" + fullPath;
            if (_fileSystem.File.Exists(withLeadingSlash))
            {
                return withLeadingSlash;
            }

            // Try with original Windows separators
            var withBackslashes = fullPath.Replace('/', '\\');
            if (_fileSystem.File.Exists(withBackslashes))
            {
                return withBackslashes;
            }

            // Try with both leading slash and backslashes
            var withBoth = "/" + withBackslashes;
            if (_fileSystem.File.Exists(withBoth))
            {
                return withBoth;
            }

            // For MockFileSystem, try to resolve relative paths manually
            // when Path.Combine doesn't handle them properly
            if (
                relativeProjectPath.StartsWith("../", StringComparison.Ordinal)
                || relativeProjectPath.StartsWith(@"..\", StringComparison.Ordinal)
                || relativeProjectPath.StartsWith("./", StringComparison.Ordinal)
                || relativeProjectPath.StartsWith(@".\", StringComparison.Ordinal)
            )
            {
                // Handle parent directory navigation manually
                var resolvedPath = ResolveRelativePathManually(
                    solutionDirectoryPath,
                    relativeProjectPath
                );
                if (!string.IsNullOrEmpty(resolvedPath))
                {
                    return resolvedPath;
                }
            }
        }

        return fullPath;
    }

    /// <summary>
    /// Manually resolves relative paths for MockFileSystem compatibility.
    /// </summary>
    private static readonly char[] PathSeparators = ['\\', '/'];

    private string ResolveRelativePathManually(string basePath, string relativePath)
    {
        var pathParts = relativePath.Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries);
        var baseParts = basePath.Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in pathParts)
        {
            if (part == "..")
            {
                if (baseParts.Length > 0)
                {
                    // Remove the last directory from base path
                    baseParts = baseParts.Take(baseParts.Length - 1).ToArray();
                }
            }
            else if (part != ".")
            {
                // Add the directory/file to the path
                baseParts = baseParts.Append(part).ToArray();
            }
        }

        var resolvedPath = string.Join("\\", baseParts);

        // Try different formats for MockFileSystem compatibility
        if (_fileSystem.File.Exists(resolvedPath))
        {
            return resolvedPath;
        }

        var withLeadingSlash = "/" + resolvedPath;
        if (_fileSystem.File.Exists(withLeadingSlash))
        {
            return withLeadingSlash;
        }

        return string.Empty;
    }

    private static bool IsSolutionFile(string extension)
    {
        return SolutionFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    private async Task ParseSlnFileAsync(
        Solution solution,
        string solutionFilePath,
        CancellationToken cancellationToken
    )
    {
        var content = await _fileSystem
            .File.ReadAllTextAsync(solutionFilePath, cancellationToken)
            .ConfigureAwait(false);
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Fix for MockFileSystem GetDirectoryName issue on Unix with Windows paths
        var solutionDirectory = _fileSystem.Path.GetDirectoryName(solutionFilePath);
        if (string.IsNullOrEmpty(solutionDirectory))
        {
            // Extract directory manually if GetDirectoryName fails
            var lastSeparator = Math.Max(
                solutionFilePath.LastIndexOf('\\'),
                solutionFilePath.LastIndexOf('/')
            );
            solutionDirectory =
                lastSeparator > 0 ? solutionFilePath.Substring(0, lastSeparator) : string.Empty;
        }

        foreach (var line in lines)
        {
            var match = ProjectLineRegex.Match(line.Trim());
            if (match.Success)
            {
                var projectName = match.Groups[1].Value;
                var relativePath = match.Groups[2].Value;

                // Normalize path separators for cross-platform compatibility
                relativePath = relativePath.Replace('\\', Path.DirectorySeparatorChar);
                var fullPath = ResolveProjectPath(solutionDirectory, relativePath);

                if (_fileSystem.File.Exists(fullPath))
                {
                    // Create a basic project entity - full loading will be done by ProjectRepository
                    var project = new Project(fullPath, projectName, "net9.0"); // Default framework
                    solution.AddProject(project);
                }
            }
        }
    }

    private async Task ParseSlnxFileAsync(
        Solution solution,
        string solutionFilePath,
        CancellationToken cancellationToken
    )
    {
        var content = await _fileSystem
            .File.ReadAllTextAsync(solutionFilePath, cancellationToken)
            .ConfigureAwait(false);

        // Fix for MockFileSystem GetDirectoryName issue on Unix with Windows paths
        var solutionDirectory = _fileSystem.Path.GetDirectoryName(solutionFilePath);
        if (string.IsNullOrEmpty(solutionDirectory))
        {
            // Extract directory manually if GetDirectoryName fails
            var lastSeparator = Math.Max(
                solutionFilePath.LastIndexOf('\\'),
                solutionFilePath.LastIndexOf('/')
            );
            solutionDirectory =
                lastSeparator > 0 ? solutionFilePath.Substring(0, lastSeparator) : string.Empty;
        }

        try
        {
            var document = XDocument.Parse(content);

            // Handle XML namespaces properly
            // Check if there's a default namespace
            var defaultNamespace = document.Root?.GetDefaultNamespace();
            XElement[] projectElements;

            if (defaultNamespace != null && !string.IsNullOrEmpty(defaultNamespace.NamespaceName))
            {
                // Use namespace-aware queries when namespace is present
                projectElements = document.Descendants(defaultNamespace + "Project").ToArray();
            }
            else
            {
                // Use simple element names when no namespace
                projectElements = document.Descendants("Project").ToArray();
            }

            // SLNX files use Project elements with Path child elements
            foreach (var projectElement in projectElements)
            {
                // Try to find Path child element first (test format)
                var pathElement =
                    defaultNamespace != null
                    && !string.IsNullOrEmpty(defaultNamespace.NamespaceName)
                        ? projectElement.Element(defaultNamespace + "Path")
                        : projectElement.Element("Path");
                var relativePath = pathElement?.Value;

                // Fallback: try Path attribute (alternative format)
                if (string.IsNullOrEmpty(relativePath))
                {
                    var pathAttribute = projectElement.Attribute("Path");
                    relativePath = pathAttribute?.Value;
                }

                if (string.IsNullOrWhiteSpace(relativePath))
                {
                    LogMissingProjectPath(solutionFilePath);
                    continue;
                }

                // Normalize path separators for cross-platform compatibility
                relativePath = relativePath.Replace('\\', Path.DirectorySeparatorChar);
                var fullPath = ResolveProjectPath(solutionDirectory, relativePath);

                // Extract project name properly - handle both Windows and Unix paths
                var normalizedPath = fullPath.Replace('\\', Path.DirectorySeparatorChar);
                var projectName = Path.GetFileNameWithoutExtension(normalizedPath);

                if (_fileSystem.File.Exists(fullPath))
                {
                    var project = new Project(fullPath, projectName, "net9.0");
                    solution.AddProject(project);
                    LogAddedProject(projectName, relativePath);
                }
                else
                {
                    LogProjectFileNotFound(fullPath);
                }
            }
        }
        catch (XmlException ex)
        {
            LogInvalidSlnxFormat(ex, solutionFilePath);
            throw new InvalidDataException($"Invalid .slnx file format: {solutionFilePath}", ex);
        }
    }

    [GeneratedRegex(
        @"Project\(""\{[^}]+\}""\)\s*=\s*""([^""]+)"",\s*""([^""]+)""",
        RegexOptions.Compiled
    )]
    private static partial Regex MyRegex();
}
