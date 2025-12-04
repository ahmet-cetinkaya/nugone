using System.IO.Abstractions;
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
        _logger.LogDebug("Discovering solution files in: {RootPath}", rootPath);

        if (!_fileSystem.Directory.Exists(rootPath))
        {
            _logger.LogWarning("Root path does not exist: {RootPath}", rootPath);
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
                    _logger.LogDebug("Found solution file: {SolutionFile}", file.FullName);
                }
            }
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            _logger.LogError(ex, "Error discovering solution files in: {RootPath}", rootPath);
            throw;
        }

        _logger.LogInformation(
            "Discovered {Count} solution file(s) in: {RootPath}",
            solutionFiles.Count,
            rootPath
        );
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
        _logger.LogDebug("Loading solution: {SolutionFilePath}", solutionFilePath);

        if (!_fileSystem.File.Exists(solutionFilePath))
            throw new FileNotFoundException($"Solution file not found: {solutionFilePath}");

        try
        {
            var solutionName = _fileSystem.Path.GetFileNameWithoutExtension(solutionFilePath);
            var solution = new Solution(solutionFilePath, solutionName);

            // Check for central package management
            var solutionDirectory =
                _fileSystem.Path.GetDirectoryName(solutionFilePath) ?? string.Empty;
            var (isEnabled, directoryPackagesPropsPath) = await CheckCentralPackageManagementAsync(
                solutionDirectory,
                cancellationToken
            );

            if (isEnabled && !string.IsNullOrEmpty(directoryPackagesPropsPath))
            {
                solution.EnableCentralPackageManagement(directoryPackagesPropsPath);
            }

            // Parse project references based on solution file type
            if (solutionFilePath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
            {
                await ParseSlnxFileAsync(solution, solutionFilePath, cancellationToken);
            }
            else
            {
                await ParseSlnFileAsync(solution, solutionFilePath, cancellationToken);
            }

            _logger.LogDebug(
                "Loaded solution: {SolutionName} with {ProjectCount} project(s)",
                solutionName,
                solution.Projects.Count
            );
            return solution;
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            _logger.LogError(ex, "Error loading solution: {SolutionFilePath}", solutionFilePath);
            throw;
        }
    }

    /// <summary>
    /// Checks if central package management is enabled for a solution.
    /// RFC-0002: Central package management detection.
    /// </summary>
    public async Task<(
        bool IsEnabled,
        string? DirectoryPackagesPropsPath
    )> CheckCentralPackageManagementAsync(
        string solutionDirectoryPath,
        CancellationToken cancellationToken = default
    )
    {
        var currentDirectory = solutionDirectoryPath;
        string? directoryPackagesPropsPath = null;

        while (!string.IsNullOrEmpty(currentDirectory))
        {
            var path = _fileSystem.Path.Combine(currentDirectory, "Directory.Packages.props");
            if (_fileSystem.File.Exists(path))
            {
                directoryPackagesPropsPath = path;
                break;
            }

            currentDirectory = _fileSystem.Path.GetDirectoryName(currentDirectory);
        }

        if (directoryPackagesPropsPath == null)
            return (false, null);

        try
        {
            var content = await _fileSystem.File.ReadAllTextAsync(
                directoryPackagesPropsPath,
                cancellationToken
            );
            var document = XDocument.Parse(content);

            var manageCentrallyElement = document
                .Descendants("ManagePackageVersionsCentrally")
                .FirstOrDefault();
            var isEnabled =
                manageCentrallyElement?.Value.Equals("true", StringComparison.OrdinalIgnoreCase)
                == true;

            return (isEnabled, isEnabled ? directoryPackagesPropsPath : null);
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            _logger.LogWarning(
                ex,
                "Error checking central package management in: {DirectoryPackagesPropsPath}",
                directoryPackagesPropsPath
            );
            return (false, null);
        }
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
        _logger.LogDebug(
            "Loading central package versions from: {DirectoryPackagesPropsPath}",
            directoryPackagesPropsPath
        );

        if (!_fileSystem.File.Exists(directoryPackagesPropsPath))
            throw new FileNotFoundException(
                $"Directory.Packages.props not found: {directoryPackagesPropsPath}"
            );

        try
        {
            var content = await _fileSystem.File.ReadAllTextAsync(
                directoryPackagesPropsPath,
                cancellationToken
            );
            var document = XDocument.Parse(content);

            var packageVersions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var packageVersionElement in document.Descendants("PackageVersion"))
            {
                var include = packageVersionElement.Attribute("Include")?.Value;
                var version = packageVersionElement.Attribute("Version")?.Value;

                if (!string.IsNullOrWhiteSpace(include) && !string.IsNullOrWhiteSpace(version))
                {
                    packageVersions[include] = version;
                }
            }

            _logger.LogDebug("Loaded {Count} central package version(s)", packageVersions.Count);
            return packageVersions;
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            _logger.LogError(
                ex,
                "Error loading central package versions from: {DirectoryPackagesPropsPath}",
                directoryPackagesPropsPath
            );
            throw;
        }
    }

    /// <summary>
    /// Resolves the full path to a project file relative to the solution directory.
    /// </summary>
    public string ResolveProjectPath(string solutionDirectoryPath, string relativeProjectPath)
    {
        return _fileSystem.Path.Combine(solutionDirectoryPath, relativeProjectPath);
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
        var content = await _fileSystem.File.ReadAllTextAsync(solutionFilePath, cancellationToken);
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var solutionDirectory = _fileSystem.Path.GetDirectoryName(solutionFilePath) ?? string.Empty;

        foreach (var line in lines)
        {
            var match = ProjectLineRegex.Match(line.Trim());
            if (match.Success)
            {
                var projectName = match.Groups[1].Value;
                var relativePath = match.Groups[2].Value;
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
        var content = await _fileSystem.File.ReadAllTextAsync(solutionFilePath, cancellationToken);
        var solutionDirectory = _fileSystem.Path.GetDirectoryName(solutionFilePath) ?? string.Empty;

        try
        {
            var document = XDocument.Parse(content);

            // SLNX files use Project elements with Path attributes directly, not nested Path elements
            foreach (var projectElement in document.Descendants("Project"))
            {
                var pathAttribute = projectElement.Attribute("Path");
                if (pathAttribute == null)
                {
                    _logger.LogWarning(
                        "Project entry missing Path attribute in: {SolutionFilePath}",
                        solutionFilePath
                    );
                    continue;
                }

                var relativePath = pathAttribute.Value;
                if (string.IsNullOrWhiteSpace(relativePath))
                {
                    _logger.LogWarning(
                        "Empty project path in: {SolutionFilePath}",
                        solutionFilePath
                    );
                    continue;
                }

                var fullPath = ResolveProjectPath(solutionDirectory, relativePath);
                var projectName = _fileSystem.Path.GetFileNameWithoutExtension(fullPath);

                if (_fileSystem.File.Exists(fullPath))
                {
                    var project = new Project(fullPath, projectName, "net9.0");
                    solution.AddProject(project);
                    _logger.LogDebug(
                        "Added project: {ProjectName} from {RelativePath}",
                        projectName,
                        relativePath
                    );
                }
                else
                {
                    _logger.LogWarning("Project file not found: {ProjectPath}", fullPath);
                }
            }
        }
        catch (XmlException ex)
        {
            _logger.LogError(
                ex,
                "Invalid XML format in .slnx file: {SolutionFilePath}",
                solutionFilePath
            );
            throw new InvalidDataException($"Invalid .slnx file format: {solutionFilePath}", ex);
        }
    }

    [GeneratedRegex(
        @"Project\(""{[^}]+}""\)\s*=\s*""([^""]+)"",\s*""([^""]+)""",
        RegexOptions.Compiled
    )]
    private static partial Regex MyRegex();
}
