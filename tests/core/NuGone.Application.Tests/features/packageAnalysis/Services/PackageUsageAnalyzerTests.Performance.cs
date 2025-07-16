using System.Diagnostics;
using NuGone.Domain.Features.PackageAnalysis.Entities;
using Shouldly;
using Xunit;

namespace NuGone.Application.Tests.Features.PackageAnalysis.Services;

/// <summary>
/// Tests for performance requirements, edge cases, and error handling scenarios.
/// Validates RFC-0002 performance targets while ensuring RFC-0004 safety constraints.
/// </summary>
public partial class PackageUsageAnalyzerTests
{
    [Fact]
    public async Task AnalyzePackageUsageAsync_WithLargeSolution_ShouldCompleteWithinPerformanceTarget()
    {
        // Arrange - RFC-0002: Target analysis completion under 2 minutes for 5,000+ files
        var projects = new List<Project>();
        var sourceFiles = new List<string>();

        // Create 10 projects with 500 files each (5,000 total files)
        for (int i = 0; i < 10; i++)
        {
            var packageRef = CreateTestPackageReference($"Package.{i}", "1.0.0");
            var project = CreateTestProject($"Project{i}", "net9.0", packageRef);
            projects.Add(project);

            var projectFiles = new List<string>();
            for (int j = 0; j < 500; j++)
            {
                var filePath = $"/test/Project{i}/File{j}.cs";
                projectFiles.Add(filePath);
                sourceFiles.Add(filePath);

                // Setup file content - some files use the package, some don't
                var content =
                    j % 10 == 0
                        ? $"using Package.{i};\nvar obj = new Class{i}();"
                        : "using System;\nConsole.WriteLine(\"Hello\");";
                SetupMockFileContent(filePath, content);
            }

            SetupMockSourceFiles(project, projectFiles.ToArray());
            SetupMockPackageNamespaces($"Package.{i}", "1.0.0", "net9.0", $"Package.{i}");
        }

        var solution = CreateTestSolution("LargeSolution", projects.ToArray());
        SetupMockPathExists(solution.FilePath);
        foreach (var project in projects)
        {
            SetupMockPathExists(project.FilePath, project.DirectoryPath);
        }

        // Act
        var stopwatch = Stopwatch.StartNew();
        await _analyzer.AnalyzePackageUsageAsync(solution);
        stopwatch.Stop();

        // Assert - Performance target: under 2 minutes for 5,000+ files
        stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromMinutes(2));

        // Verify analysis completed correctly
        foreach (var project in projects)
        {
            var packageRef = project.PackageReferences.First();
            packageRef.IsUsed.ShouldBeTrue(); // Each package should be used in some files
        }
    }

    [Fact]
    public async Task AnalyzePackageUsageAsync_WithComplexNamespacePatterns_ShouldHandleCorrectly()
    {
        // Arrange - Edge case: Complex namespace patterns and wildcard matching
        var packageRef = CreateTestPackageReference("Complex.Package", "1.0.0");
        var project = CreateTestProject("TestProject", "net9.0", packageRef);
        var solution = CreateTestSolution("TestSolution", project);

        SetupMockSourceFiles(project, "/test/ComplexUsage.cs");
        SetupMockFileContent(
            "/test/ComplexUsage.cs",
            @"
            using Complex.Package.Core;
            using Complex.Package.Extensions.Helpers;
            using System;
            
            namespace MyApp
            {
                public class Handler
                {
                    public void Process()
                    {
                        var core = new Complex.Package.Core.CoreClass();
                        Complex.Package.Utilities.Helper.DoSomething();
                    }
                }
            }"
        );

        SetupMockPackageNamespaces(
            "Complex.Package",
            "1.0.0",
            "net9.0",
            "Complex.Package",
            "Complex.Package.Core",
            "Complex.Package.Extensions",
            "Complex.Package.Utilities"
        );
        SetupMockPathExists(solution.FilePath, project.FilePath, project.DirectoryPath);

        // Act
        await _analyzer.AnalyzePackageUsageAsync(solution);

        // Assert
        packageRef.IsUsed.ShouldBeTrue();
        packageRef.DetectedNamespaces.ShouldContain("Complex.Package.Core");
        packageRef.DetectedNamespaces.ShouldContain("Complex.Package.Extensions");
        packageRef.DetectedNamespaces.ShouldContain("Complex.Package.Utilities");
    }

    [Fact]
    public async Task AnalyzePackageUsageAsync_WithMultiTargetedProject_ShouldHandleAllTargets()
    {
        // Arrange - RFC-0002: Support multi-targeted projects
        var packageRef = CreateTestPackageReference("MultiTarget.Package", "1.0.0");
        var project = CreateTestProject("MultiTargetProject", "net9.0;net8.0", packageRef);
        var solution = CreateTestSolution("TestSolution", project);

        SetupMockSourceFiles(project, "/test/MultiTarget.cs");
        SetupMockFileContent(
            "/test/MultiTarget.cs",
            @"
            #if NET9_0
            using MultiTarget.Package.Net9;
            #elif NET8_0
            using MultiTarget.Package.Net8;
            #endif
            using MultiTarget.Package.Common;"
        );

        SetupMockPackageNamespaces(
            "MultiTarget.Package",
            "1.0.0",
            "net9.0",
            "MultiTarget.Package.Net9",
            "MultiTarget.Package.Common"
        );
        SetupMockPackageNamespaces(
            "MultiTarget.Package",
            "1.0.0",
            "net8.0",
            "MultiTarget.Package.Net8",
            "MultiTarget.Package.Common"
        );
        SetupMockPathExists(solution.FilePath, project.FilePath, project.DirectoryPath);

        // Act
        await _analyzer.AnalyzePackageUsageAsync(solution);

        // Assert
        packageRef.IsUsed.ShouldBeTrue();
        packageRef.DetectedNamespaces.ShouldContain("MultiTarget.Package.Common");
    }

    [Fact]
    public async Task AnalyzePackageUsageAsync_WithAutoGeneratedFiles_ShouldExcludeByDefault()
    {
        // Arrange - RFC-0002: Exclude known auto-generated files by default
        var packageRef = CreateTestPackageReference("Test.Package", "1.0.0");
        var project = CreateTestProject("TestProject", "net9.0", packageRef);
        var solution = CreateTestSolution("TestSolution", project);

        var sourceFiles = new[]
        {
            "/test/Normal.cs",
            "/test/Generated.Designer.cs",
            "/test/Auto.g.cs",
            "/test/Temp.g.i.cs",
        };

        SetupMockSourceFiles(project, sourceFiles);

        // All files contain package usage, but generated files should be excluded
        foreach (var file in sourceFiles)
        {
            SetupMockFileContent(file, "using Test.Package;\nvar obj = new TestClass();");
        }

        SetupMockPackageNamespaces("Test.Package", "1.0.0", "net9.0", "Test.Package");
        SetupMockPathExists(solution.FilePath, project.FilePath, project.DirectoryPath);

        // Act
        await _analyzer.AnalyzePackageUsageAsync(solution);

        // Assert
        packageRef.IsUsed.ShouldBeTrue();
        // Only normal files should be in usage locations, not generated files
        packageRef.UsageLocations.ShouldContain("/test/Normal.cs");
        packageRef.UsageLocations.ShouldNotContain("/test/Generated.Designer.cs");
        packageRef.UsageLocations.ShouldNotContain("/test/Auto.g.cs");
        packageRef.UsageLocations.ShouldNotContain("/test/Temp.g.i.cs");
    }

    [Fact]
    public async Task AnalyzePackageUsageAsync_WithDuplicatePackageReferences_ShouldHandleCorrectly()
    {
        // Arrange - Edge case: Same package referenced in multiple projects with different versions
        var package1 = CreateTestPackageReference(
            "Shared.Package",
            "1.0.0",
            "/test/Project1.csproj"
        );
        var package2 = CreateTestPackageReference(
            "Shared.Package",
            "2.0.0",
            "/test/Project2.csproj"
        );

        var project1 = CreateTestProject("Project1", "net9.0", package1);
        var project2 = CreateTestProject("Project2", "net9.0", package2);
        var solution = CreateTestSolution("TestSolution", project1, project2);

        SetupMockSourceFiles(project1, "/test/Project1/Class1.cs");
        SetupMockSourceFiles(project2, "/test/Project2/Class2.cs");

        SetupMockFileContent(
            "/test/Project1/Class1.cs",
            "using Shared.Package;\nvar obj = new SharedClass();"
        );
        SetupMockFileContent(
            "/test/Project2/Class2.cs",
            "using System;\nConsole.WriteLine(\"Hello\");"
        );

        SetupMockPackageNamespaces("Shared.Package", "1.0.0", "net9.0", "Shared.Package");
        SetupMockPackageNamespaces("Shared.Package", "2.0.0", "net9.0", "Shared.Package");
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

        // RFC-0004 Safety: Different versions should be handled independently
        var packageGroups = solution.GetPackageReferencesGroupedById();
        packageGroups.ShouldContainKey("Shared.Package");
        packageGroups["Shared.Package"].Count.ShouldBe(2);
    }

    [Fact]
    public async Task AnalyzePackageUsageAsync_WithMemoryConstraints_ShouldNotExceedLimits()
    {
        // Arrange - RFC-0002: Target memory overhead < 50 MB additional memory usage
        var initialMemory = GC.GetTotalMemory(true);

        var packages = Enumerable
            .Range(0, 100)
            .Select(i => CreateTestPackageReference($"Package.{i}", "1.0.0"))
            .ToArray();

        var project = CreateTestProject("MemoryTestProject", "net9.0", packages);
        var solution = CreateTestSolution("MemoryTestSolution", project);

        var sourceFiles = Enumerable.Range(0, 1000).Select(i => $"/test/File{i}.cs").ToArray();

        SetupMockSourceFiles(project, sourceFiles);

        foreach (var file in sourceFiles)
        {
            SetupMockFileContent(file, "using System;\nConsole.WriteLine(\"Hello\");");
        }

        foreach (var package in packages)
        {
            SetupMockPackageNamespaces(
                package.PackageId,
                package.Version,
                "net9.0",
                package.PackageId
            );
        }

        SetupMockPathExists(solution.FilePath, project.FilePath, project.DirectoryPath);

        // Act
        await _analyzer.AnalyzePackageUsageAsync(solution);

        var finalMemory = GC.GetTotalMemory(true);
        var memoryUsed = finalMemory - initialMemory;

        // Assert - Memory usage should be reasonable (allowing for test overhead)
        memoryUsed.ShouldBeLessThan(100 * 1024 * 1024); // 100 MB limit for test

        // Verify analysis completed
        packages.All(p => !p.IsUsed).ShouldBeTrue(); // All packages unused in this test
    }

    [Fact]
    public async Task AnalyzePackageUsageAsync_WithConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange - Edge case: Concurrent analysis operations
        var packageRef = CreateTestPackageReference("Concurrent.Package", "1.0.0");
        var project = CreateTestProject("ConcurrentProject", "net9.0", packageRef);
        var solution = CreateTestSolution("ConcurrentSolution", project);

        SetupMockSourceFiles(project, "/test/Concurrent.cs");
        SetupMockFileContent(
            "/test/Concurrent.cs",
            "using Concurrent.Package;\nvar obj = new ConcurrentClass();"
        );
        SetupMockPackageNamespaces("Concurrent.Package", "1.0.0", "net9.0", "Concurrent.Package");
        SetupMockPathExists(solution.FilePath, project.FilePath, project.DirectoryPath);

        // Act - Run multiple concurrent analyses
        var tasks = Enumerable
            .Range(0, 10)
            .Select(_ => _analyzer.AnalyzePackageUsageAsync(solution))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert - All analyses should complete successfully
        packageRef.IsUsed.ShouldBeTrue();
        packageRef.UsageLocations.ShouldContain("/test/Concurrent.cs");
    }
}
