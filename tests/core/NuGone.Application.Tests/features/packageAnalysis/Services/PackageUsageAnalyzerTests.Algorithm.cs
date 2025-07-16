using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace NuGone.Application.Tests.Features.PackageAnalysis.Services;

/// <summary>
/// Tests for core algorithm functionality with safety integration.
/// Validates RFC-0002 unused package detection algorithm while ensuring RFC-0004 safety compliance.
/// </summary>
public partial class PackageUsageAnalyzerTests
{
    [Fact]
    public async Task AnalyzePackageUsageAsync_WithUsedPackage_ShouldMarkPackageAsUsed()
    {
        // Arrange
        var packageRef = CreateTestPackageReference("Newtonsoft.Json", "13.0.3");
        var project = CreateTestProject("TestProject", "net9.0", packageRef);
        var solution = CreateTestSolution("TestSolution", project);

        SetupMockSourceFiles(project, "/test/Program.cs");
        SetupMockFileContent(
            "/test/Program.cs",
            "using Newtonsoft.Json;\nvar obj = JsonConvert.SerializeObject(data);"
        );
        SetupMockPackageNamespaces("Newtonsoft.Json", "13.0.3", "net9.0", "Newtonsoft.Json");
        SetupMockPathExists(solution.FilePath, project.FilePath, project.DirectoryPath);

        // Act
        await _analyzer.AnalyzePackageUsageAsync(solution);

        // Assert
        packageRef.IsUsed.ShouldBeTrue();
        packageRef.UsageLocations.ShouldContain("/test/Program.cs");
        packageRef.DetectedNamespaces.ShouldContain("Newtonsoft.Json");

        // RFC-0004 Safety: Verify package is correctly identified as used (safe to keep)
        solution.GetAllUsedPackages().ShouldContain(packageRef);
        solution.GetAllUnusedPackages().ShouldNotContain(packageRef);
    }

    [Fact]
    public async Task AnalyzePackageUsageAsync_WithUnusedPackage_ShouldMarkPackageAsUnused()
    {
        // Arrange
        var packageRef = CreateTestPackageReference("Unused.Package", "1.0.0");
        var project = CreateTestProject("TestProject", "net9.0", packageRef);
        var solution = CreateTestSolution("TestSolution", project);

        SetupMockSourceFiles(project, "/test/Program.cs");
        SetupMockFileContent(
            "/test/Program.cs",
            "using System;\nConsole.WriteLine(\"Hello World\");"
        );
        SetupMockPackageNamespaces("Unused.Package", "1.0.0", "net9.0", "Unused.Package");
        SetupMockPathExists(solution.FilePath, project.FilePath, project.DirectoryPath);

        // Act
        await _analyzer.AnalyzePackageUsageAsync(solution);

        // Assert
        packageRef.IsUsed.ShouldBeFalse();
        packageRef.UsageLocations.ShouldBeEmpty();
        packageRef.DetectedNamespaces.ShouldBeEmpty();

        // RFC-0004 Safety: Verify package is correctly identified as unused (candidate for removal)
        solution.GetAllUnusedPackages().ShouldContain(packageRef);
        solution.GetAllUsedPackages().ShouldNotContain(packageRef);
    }

    [Fact]
    public async Task AnalyzePackageUsageAsync_WithMultipleProjects_ShouldAnalyzeAllProjects()
    {
        // Arrange
        var package1 = CreateTestPackageReference("Package.One", "1.0.0");
        var package2 = CreateTestPackageReference("Package.Two", "2.0.0");

        var project1 = CreateTestProject("Project1", "net9.0", package1);
        var project2 = CreateTestProject("Project2", "net9.0", package2);
        var solution = CreateTestSolution("TestSolution", project1, project2);

        // Setup Project1 - uses Package.One
        SetupMockSourceFiles(project1, "/test/Project1/Class1.cs");
        SetupMockFileContent(
            "/test/Project1/Class1.cs",
            "using Package.One;\nvar obj = new OneClass();"
        );
        SetupMockPackageNamespaces("Package.One", "1.0.0", "net9.0", "Package.One");

        // Setup Project2 - doesn't use Package.Two
        SetupMockSourceFiles(project2, "/test/Project2/Class2.cs");
        SetupMockFileContent(
            "/test/Project2/Class2.cs",
            "using System;\nConsole.WriteLine(\"Hello\");"
        );
        SetupMockPackageNamespaces("Package.Two", "2.0.0", "net9.0", "Package.Two");

        SetupMockPathExists(
            solution.FilePath,
            project1.FilePath,
            project1.DirectoryPath,
            project2.FilePath,
            project2.DirectoryPath
        );

        // Act
        await _analyzer.AnalyzePackageUsageAsync(solution);

        // Assert
        package1.IsUsed.ShouldBeTrue();
        package2.IsUsed.ShouldBeFalse();

        // RFC-0004 Safety: Verify correct classification for removal safety
        var usedPackages = solution.GetAllUsedPackages().ToList();
        var unusedPackages = solution.GetAllUnusedPackages().ToList();

        usedPackages.ShouldContain(package1);
        usedPackages.ShouldNotContain(package2);
        unusedPackages.ShouldContain(package2);
        unusedPackages.ShouldNotContain(package1);
    }

    [Fact]
    public async Task AnalyzePackageUsageAsync_WithNamespaceUsageInCode_ShouldDetectUsage()
    {
        // Arrange
        var packageRef = CreateTestPackageReference("System.Text.Json", "8.0.0");
        var project = CreateTestProject("TestProject", "net9.0", packageRef);
        var solution = CreateTestSolution("TestSolution", project);

        SetupMockSourceFiles(project, "/test/JsonHandler.cs");
        SetupMockFileContent(
            "/test/JsonHandler.cs",
            "public class JsonHandler\n{\n    public string Serialize(object obj)\n    {\n        return System.Text.Json.JsonSerializer.Serialize(obj);\n    }\n}"
        );
        SetupMockPackageNamespaces("System.Text.Json", "8.0.0", "net9.0", "System.Text.Json");
        SetupMockPathExists(solution.FilePath, project.FilePath, project.DirectoryPath);

        // Act
        await _analyzer.AnalyzePackageUsageAsync(solution);

        // Assert
        packageRef.IsUsed.ShouldBeTrue();
        packageRef.DetectedNamespaces.ShouldContain("System.Text.Json");

        // RFC-0004 Safety: Package with direct namespace usage should be safe to keep
        solution.GetAllUsedPackages().ShouldContain(packageRef);
    }

    [Fact]
    public async Task AnalyzePackageUsageAsync_WithConditionalPackageReference_ShouldHandleCorrectly()
    {
        // Arrange - RFC-0002 specifies handling conditional references
        var conditionalPackage = CreateTestPackageReference(
            "Microsoft.EntityFrameworkCore.Tools",
            "8.0.0",
            "/test/project.csproj",
            true,
            "'$(Configuration)' == 'Debug'"
        );

        var project = CreateTestProject("TestProject", "net9.0", conditionalPackage);
        var solution = CreateTestSolution("TestSolution", project);

        SetupMockSourceFiles(project, "/test/Program.cs");
        SetupMockFileContent("/test/Program.cs", "using System;\nConsole.WriteLine(\"Hello\");");
        SetupMockPackageNamespaces(
            "Microsoft.EntityFrameworkCore.Tools",
            "8.0.0",
            "net9.0",
            "Microsoft.EntityFrameworkCore.Tools"
        );
        SetupMockPathExists(solution.FilePath, project.FilePath, project.DirectoryPath);

        // Act
        await _analyzer.AnalyzePackageUsageAsync(solution);

        // Assert
        conditionalPackage.IsUsed.ShouldBeFalse();
        conditionalPackage.Condition.ShouldBe("'$(Configuration)' == 'Debug'");

        // RFC-0004 Safety: Conditional packages require special handling for safe removal
        solution.GetAllUnusedPackages().ShouldContain(conditionalPackage);
    }

    [Fact]
    public async Task AnalyzePackageUsageAsync_WithTransitiveDependency_ShouldDistinguishFromDirect()
    {
        // Arrange - RFC-0002 requires distinguishing direct vs transitive dependencies
        var directPackage = CreateTestPackageReference("Direct.Package", "1.0.0", isDirect: true);
        var transitivePackage = CreateTestPackageReference(
            "Transitive.Package",
            "1.0.0",
            isDirect: false
        );

        var project = CreateTestProject("TestProject", "net9.0", directPackage, transitivePackage);
        var solution = CreateTestSolution("TestSolution", project);

        SetupMockSourceFiles(project, "/test/Program.cs");
        SetupMockFileContent(
            "/test/Program.cs",
            "using Direct.Package;\nvar obj = new DirectClass();"
        );
        SetupMockPackageNamespaces("Direct.Package", "1.0.0", "net9.0", "Direct.Package");
        SetupMockPackageNamespaces("Transitive.Package", "1.0.0", "net9.0", "Transitive.Package");
        SetupMockPathExists(solution.FilePath, project.FilePath, project.DirectoryPath);

        // Act
        await _analyzer.AnalyzePackageUsageAsync(solution);

        // Assert
        directPackage.IsUsed.ShouldBeTrue();
        directPackage.IsDirect.ShouldBeTrue();
        transitivePackage.IsUsed.ShouldBeFalse();
        transitivePackage.IsDirect.ShouldBeFalse();

        // RFC-0004 Safety: Only direct unused packages should be candidates for removal
        var unusedDirectPackages = solution.GetAllUnusedPackages().Where(p => p.IsDirect).ToList();
        var unusedTransitivePackages = solution
            .GetAllUnusedPackages()
            .Where(p => !p.IsDirect)
            .ToList();

        unusedDirectPackages.ShouldNotContain(directPackage);
        unusedTransitivePackages.ShouldContain(transitivePackage);
    }

    [Fact]
    public async Task AnalyzePackageUsageAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var packageRef = CreateTestPackageReference("Test.Package", "1.0.0");
        var project = CreateTestProject("TestProject", "net9.0", packageRef);
        var solution = CreateTestSolution("TestSolution", project);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        SetupMockSourceFiles(project, "/test/Program.cs");
        SetupMockPathExists(solution.FilePath, project.FilePath, project.DirectoryPath);

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            _analyzer.AnalyzePackageUsageAsync(solution, cts.Token)
        );
    }

    [Fact]
    public async Task AnalyzePackageUsageAsync_WithNoNamespacesFound_ShouldLogWarningAndContinue()
    {
        // Arrange
        var packageRef = CreateTestPackageReference("Empty.Package", "1.0.0");
        var project = CreateTestProject("TestProject", "net9.0", packageRef);
        var solution = CreateTestSolution("TestSolution", project);

        SetupMockSourceFiles(project, "/test/Program.cs");
        SetupMockFileContent("/test/Program.cs", "using System;\nConsole.WriteLine(\"Hello\");");
        SetupMockPackageNamespaces("Empty.Package", "1.0.0", "net9.0"); // No namespaces
        SetupMockPathExists(solution.FilePath, project.FilePath, project.DirectoryPath);

        // Act
        await _analyzer.AnalyzePackageUsageAsync(solution);

        // Assert
        packageRef.IsUsed.ShouldBeFalse();

        // Verify warning was logged
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) =>
                            v.ToString()!.Contains("No namespaces found for package: Empty.Package")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }
}
