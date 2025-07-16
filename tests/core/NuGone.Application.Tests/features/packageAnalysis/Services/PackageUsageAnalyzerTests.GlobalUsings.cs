using Microsoft.Extensions.Logging;
using Moq;
using NuGone.Application.Features.PackageAnalysis.Services;
using NuGone.Application.Features.PackageAnalysis.Services.Abstractions;
using NuGone.Domain.Features.PackageAnalysis.Entities;
using Shouldly;
using Xunit;

namespace NuGone.Application.Tests.Features.PackageAnalysis.Services;

/// <summary>
/// Tests for PackageUsageAnalyzer's global using functionality.
/// </summary>
public class PackageUsageAnalyzerGlobalUsingsTests
{
    private readonly Mock<IProjectRepository> _mockProjectRepository;
    private readonly Mock<INuGetRepository> _mockNuGetRepository;
    private readonly Mock<ILogger<PackageUsageAnalyzer>> _mockLogger;
    private readonly PackageUsageAnalyzer _analyzer;

    public PackageUsageAnalyzerGlobalUsingsTests()
    {
        _mockProjectRepository = new Mock<IProjectRepository>();
        _mockNuGetRepository = new Mock<INuGetRepository>();
        _mockLogger = new Mock<ILogger<PackageUsageAnalyzer>>();

        _analyzer = new PackageUsageAnalyzer(
            _mockProjectRepository.Object,
            _mockNuGetRepository.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task AnalyzeProjectPackageUsageAsync_WithGlobalUsings_ShouldDetectUsageCorrectly()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");

        // Add a package reference with global using
        var packageRef = new PackageReference(
            "Xunit",
            "2.4.2",
            "/path/to/project.csproj",
            hasGlobalUsing: true
        );
        project.AddPackageReference(packageRef);

        // Add a global using declaration
        var globalUsing = new GlobalUsing("Xunit", "/path/to/project.csproj");
        project.AddGlobalUsing(globalUsing);

        // Mock source files
        var sourceFiles = new List<string> { "/path/to/TestClass.cs" };
        _ = _mockProjectRepository
            .Setup(r => r.GetProjectSourceFilesAsync(project, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceFiles);

        // Mock source file content that uses Xunit without explicit using statement
        var sourceContent = """
            namespace TestProject;

            public class TestClass
            {
                [Fact] // This uses Xunit.Fact attribute via global using
                public void TestMethod()
                {
                    Assert.True(true); // This uses Xunit.Assert via global using
                }
            }
            """;

        _ = _mockProjectRepository
            .Setup(r =>
                r.ReadSourceFileAsync("/path/to/TestClass.cs", It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(sourceContent);

        // Mock package namespaces
        _ = _mockNuGetRepository
            .Setup(r =>
                r.GetPackageNamespacesAsync("Xunit", "2.4.2", "net9.0", CancellationToken.None)
            )
            .ReturnsAsync(["Xunit"]);

        // Act
        await _analyzer.AnalyzeProjectPackageUsageAsync(project, CancellationToken.None);

        // Assert
        packageRef.IsUsed.ShouldBeTrue("Package should be detected as used through global using");
        packageRef.HasGlobalUsing.ShouldBeTrue("Package should have global using flag set");
    }

    [Fact]
    public async Task AnalyzeProjectPackageUsageAsync_WithUnusedGlobalUsing_ShouldMarkAsUnused()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");

        // Add a package reference with global using but no actual usage
        var packageRef = new PackageReference(
            "Moq",
            "4.18.4",
            "/path/to/project.csproj",
            hasGlobalUsing: true
        );
        project.AddPackageReference(packageRef);

        // Add a global using declaration
        var globalUsing = new GlobalUsing("Moq", "/path/to/project.csproj");
        project.AddGlobalUsing(globalUsing);

        // Mock source files
        var sourceFiles = new List<string> { "/path/to/TestClass.cs" };
        _ = _mockProjectRepository
            .Setup(r => r.GetProjectSourceFilesAsync(project, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceFiles);

        // Mock source file content that doesn't use Moq
        var sourceContent = """
            namespace TestProject;

            public class TestClass
            {
                public void TestMethod()
                {
                    // No usage of Moq here
                    var result = 42;
                }
            }
            """;

        _ = _mockProjectRepository
            .Setup(r =>
                r.ReadSourceFileAsync("/path/to/TestClass.cs", It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(sourceContent);

        // Mock package namespaces
        _ = _mockNuGetRepository
            .Setup(r =>
                r.GetPackageNamespacesAsync("Moq", "4.18.4", "net9.0", CancellationToken.None)
            )
            .ReturnsAsync(["Moq"]);

        // Act
        await _analyzer.AnalyzeProjectPackageUsageAsync(project, CancellationToken.None);

        // Assert
        packageRef.IsUsed.ShouldBeFalse(
            "Package should be marked as unused despite having global using"
        );
        packageRef.HasGlobalUsing.ShouldBeTrue("Package should still have global using flag set");
    }

    [Fact]
    public async Task AnalyzeProjectPackageUsageAsync_WithMixedGlobalUsings_ShouldAnalyzeCorrectly()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");

        // Add packages: one with global using (used), one with global using (unused), one without global using (used)
        var xunitPackage = new PackageReference(
            "Xunit",
            "2.4.2",
            "/path/to/project.csproj",
            hasGlobalUsing: true
        );
        var moqPackage = new PackageReference(
            "Moq",
            "4.18.4",
            "/path/to/project.csproj",
            hasGlobalUsing: true
        );
        var newtonsoftPackage = new PackageReference(
            "Newtonsoft.Json",
            "13.0.3",
            "/path/to/project.csproj",
            hasGlobalUsing: false
        );

        project.AddPackageReference(xunitPackage);
        project.AddPackageReference(moqPackage);
        project.AddPackageReference(newtonsoftPackage);

        // Mock source files
        var sourceFiles = new List<string> { "/path/to/TestClass.cs" };
        _ = _mockProjectRepository
            .Setup(r => r.GetProjectSourceFilesAsync(project, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceFiles);

        // Mock source file content
        var sourceContent = """
            using Newtonsoft.Json; // Explicit using for Newtonsoft.Json

            namespace TestProject;

            public class TestClass
            {
                [Fact] // Uses Xunit via global using
                public void TestMethod()
                {
                    var json = JsonConvert.SerializeObject(new { test = true }); // Uses Newtonsoft.Json
                    Assert.True(true); // Uses Xunit via global using
                    // No usage of Moq despite global using
                }
            }
            """;

        _ = _mockProjectRepository
            .Setup(r =>
                r.ReadSourceFileAsync("/path/to/TestClass.cs", It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(sourceContent);

        // Mock package namespaces
        _ = _mockNuGetRepository
            .Setup(r =>
                r.GetPackageNamespacesAsync("Xunit", "2.4.2", "net9.0", CancellationToken.None)
            )
            .ReturnsAsync(["Xunit"]);

        _ = _mockNuGetRepository
            .Setup(r =>
                r.GetPackageNamespacesAsync("Moq", "4.18.4", "net9.0", CancellationToken.None)
            )
            .ReturnsAsync(["Moq"]);

        _ = _mockNuGetRepository
            .Setup(r =>
                r.GetPackageNamespacesAsync(
                    "Newtonsoft.Json",
                    "13.0.3",
                    "net9.0",
                    CancellationToken.None
                )
            )
            .ReturnsAsync(["Newtonsoft.Json"]);

        // Act
        await _analyzer.AnalyzeProjectPackageUsageAsync(project, CancellationToken.None);

        // Assert
        xunitPackage.IsUsed.ShouldBeTrue("Xunit should be used via global using");
        xunitPackage.HasGlobalUsing.ShouldBeTrue();

        moqPackage.IsUsed.ShouldBeFalse("Moq should be unused despite global using");
        moqPackage.HasGlobalUsing.ShouldBeTrue();

        newtonsoftPackage.IsUsed.ShouldBeTrue("Newtonsoft.Json should be used via explicit using");
        newtonsoftPackage.HasGlobalUsing.ShouldBeFalse();
    }

    [Fact]
    public async Task AnalyzeProjectPackageUsageAsync_WithConditionalGlobalUsing_ShouldAnalyzeCorrectly()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");

        // Add a package reference with conditional global using
        var packageRef = new PackageReference(
            "Xunit",
            "2.4.2",
            "/path/to/project.csproj",
            condition: "'$(Configuration)' == 'Debug'",
            hasGlobalUsing: true
        );
        project.AddPackageReference(packageRef);

        // Add a conditional global using declaration
        var globalUsing = new GlobalUsing(
            "Xunit",
            "/path/to/project.csproj",
            "'$(Configuration)' == 'Debug'"
        );
        project.AddGlobalUsing(globalUsing);

        // Mock source files
        var sourceFiles = new List<string> { "/path/to/TestClass.cs" };
        _ = _mockProjectRepository
            .Setup(r => r.GetProjectSourceFilesAsync(project, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceFiles);

        // Mock source file content that uses Xunit
        var sourceContent = """
            namespace TestProject;

            public class TestClass
            {
                [Fact]
                public void TestMethod()
                {
                    Assert.True(true);
                }
            }
            """;

        _ = _mockProjectRepository
            .Setup(r =>
                r.ReadSourceFileAsync("/path/to/TestClass.cs", It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(sourceContent);

        // Mock package namespaces
        _ = _mockNuGetRepository
            .Setup(r =>
                r.GetPackageNamespacesAsync("Xunit", "2.4.2", "net9.0", CancellationToken.None)
            )
            .ReturnsAsync(["Xunit"]);

        // Act
        await _analyzer.AnalyzeProjectPackageUsageAsync(project, CancellationToken.None);

        // Assert
        packageRef.IsUsed.ShouldBeTrue("Package should be detected as used");
        packageRef.HasGlobalUsing.ShouldBeTrue("Package should have global using flag set");
        packageRef.Condition.ShouldBe("'$(Configuration)' == 'Debug'");
    }
}
