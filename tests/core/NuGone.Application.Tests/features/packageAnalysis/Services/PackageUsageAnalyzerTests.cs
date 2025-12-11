using Microsoft.Extensions.Logging;
using Moq;
using NuGone.Application.Features.PackageAnalysis.Services;
using NuGone.Application.Features.PackageAnalysis.Services.Abstractions;
using NuGone.Domain.Features.PackageAnalysis.Entities;

namespace NuGone.Application.Tests.Features.PackageAnalysis.Services;

/// <summary>
/// Comprehensive unit tests for PackageUsageAnalyzer.
/// Tests RFC-0002 algorithm functionality with RFC-0004 safety mechanism integration.
/// Validates unused package detection accuracy and safety compliance.
/// </summary>
public partial class PackageUsageAnalyzerTests
{
    private readonly Mock<IProjectRepository> _mockProjectRepository;
    private readonly Mock<INuGetRepository> _mockNuGetRepository;
    private readonly Mock<ILogger<PackageUsageAnalyzer>> _mockLogger;
    private readonly PackageUsageAnalyzer _analyzer;

    public PackageUsageAnalyzerTests()
    {
        _mockProjectRepository = new Mock<IProjectRepository>();
        _mockNuGetRepository = new Mock<INuGetRepository>();
        _mockLogger = new Mock<ILogger<PackageUsageAnalyzer>>();
        // Enable log levels so LoggerMessage source generator actually logs
        _mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        _analyzer = new PackageUsageAnalyzer(
            _mockProjectRepository.Object,
            _mockNuGetRepository.Object,
            _mockLogger.Object
        );
    }

    /// <summary>
    /// Creates a test solution with specified projects for testing.
    /// </summary>
    private static Solution CreateTestSolution(
        string name = "TestSolution",
        params Project[] projects
    )
    {
        var solution = new Solution($"/test/path/{name}.sln", name);
        foreach (var project in projects)
        {
            solution.AddProject(project);
        }
        return solution;
    }

    /// <summary>
    /// Creates a test project with specified package references.
    /// </summary>
    private static Project CreateTestProject(
        string name = "TestProject",
        string targetFramework = "net9.0",
        params PackageReference[] packageReferences
    )
    {
        var project = new Project($"/test/path/{name}.csproj", name, targetFramework);
        foreach (var packageRef in packageReferences)
        {
            project.AddPackageReference(packageRef);
        }
        return project;
    }

    /// <summary>
    /// Creates a test package reference.
    /// </summary>
    private static PackageReference CreateTestPackageReference(
        string packageId,
        string version = "1.0.0",
        string projectPath = "/test/project.csproj",
        bool isDirect = true,
        string? condition = null
    )
    {
        return new PackageReference(packageId, version, projectPath, isDirect, condition);
    }

    /// <summary>
    /// Sets up mock repository to return specified source files for a project.
    /// </summary>
    private void SetupMockSourceFiles(Project project, params string[] sourceFiles)
    {
        _ = _mockProjectRepository
            .Setup(r => r.GetProjectSourceFilesAsync(project, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceFiles);
    }

    /// <summary>
    /// Sets up mock repository to return specified content for a source file.
    /// </summary>
    private void SetupMockFileContent(string filePath, string content)
    {
        _ = _mockProjectRepository
            .Setup(r => r.ReadSourceFileAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);
    }

    /// <summary>
    /// Sets up mock NuGet repository to return specified namespaces for a package.
    /// </summary>
    private void SetupMockPackageNamespaces(
        string packageId,
        string version,
        string targetFramework,
        params string[] namespaces
    )
    {
        _ = _mockNuGetRepository
            .Setup(r =>
                r.GetPackageNamespacesAsync(
                    packageId,
                    version,
                    targetFramework,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(namespaces);
    }

    /// <summary>
    /// Sets up mock repository to indicate paths exist.
    /// </summary>
    private void SetupMockPathExists(params string[] paths)
    {
        foreach (var path in paths)
        {
            _ = _mockProjectRepository.Setup(r => r.ExistsAsync(path)).ReturnsAsync(true);
        }
    }
}
