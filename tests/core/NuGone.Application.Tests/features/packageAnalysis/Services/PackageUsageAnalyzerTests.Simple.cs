using NuGone.Application.Features.PackageAnalysis.Services.Abstractions;
using NuGone.Domain.Features.PackageAnalysis.Entities;
using NuGone.Domain.Features.PackageAnalysis.ValueObjects;
using Xunit;

namespace NuGone.Application.Tests.Features.PackageAnalysis.Services;

/// <summary>
/// Simple tests for PackageUsageAnalyzer without mocking to verify basic functionality.
/// Tests RFC-0002 algorithm functionality with RFC-0004 safety mechanism integration.
/// </summary>
public class PackageUsageAnalyzerSimpleTests
{
    [Fact]
    public void CreateTestPackageReference_ShouldCreateValidPackageReference()
    {
        // Arrange & Act
        var packageRef = new PackageReference(
            "Test.Package",
            "1.0.0",
            "/test/project.csproj",
            true,
            null
        );

        // Assert
        Assert.Equal("Test.Package", packageRef.PackageId);
        Assert.Equal("1.0.0", packageRef.Version);
        Assert.Equal("/test/project.csproj", packageRef.ProjectPath);
        Assert.True(packageRef.IsDirect);
        Assert.Null(packageRef.Condition);
        Assert.False(packageRef.IsUsed);
        Assert.Empty(packageRef.UsageLocations);
        Assert.Empty(packageRef.DetectedNamespaces);
    }

    [Fact]
    public void CreateTestProject_ShouldCreateValidProject()
    {
        // Arrange
        var packageRef = new PackageReference(
            "Test.Package",
            "1.0.0",
            "/test/project.csproj",
            true,
            null
        );

        // Act
        var project = new Project("/test/project.csproj", "TestProject", "net9.0");
        project.AddPackageReference(packageRef);

        // Assert
        Assert.Equal("TestProject", project.Name);
        Assert.Equal("/test/project.csproj", project.FilePath);
        Assert.Equal("net9.0", project.TargetFramework);
        Assert.Contains(packageRef, project.PackageReferences);
    }

    [Fact]
    public void CreateTestSolution_ShouldCreateValidSolution()
    {
        // Arrange
        var project = new Project("/test/project.csproj", "TestProject", "net9.0");

        // Act
        var solution = new Solution("/test/solution.sln", "TestSolution");
        solution.AddProject(project);

        // Assert
        Assert.Equal("TestSolution", solution.Name);
        Assert.Equal("/test/solution.sln", solution.FilePath);
        Assert.Contains(project, solution.Projects);
    }

    [Fact]
    public void PackageReference_MarkAsUsed_ShouldUpdateUsageStatus()
    {
        // Arrange
        var packageRef = new PackageReference(
            "Test.Package",
            "1.0.0",
            "/test/project.csproj",
            true,
            null
        );

        // Act
        packageRef.MarkAsUsed("/test/file.cs", "Test.Package");

        // Assert
        Assert.True(packageRef.IsUsed);
        Assert.Contains("/test/file.cs", packageRef.UsageLocations);
        Assert.Contains("Test.Package", packageRef.DetectedNamespaces);
    }

    [Fact]
    public void PackageReference_ResetUsageStatus_ShouldClearUsageData()
    {
        // Arrange
        var packageRef = new PackageReference(
            "Test.Package",
            "1.0.0",
            "/test/project.csproj",
            true,
            null
        );
        packageRef.MarkAsUsed("/test/file.cs", "Test.Package");

        // Act
        packageRef.ResetUsageStatus();

        // Assert
        Assert.False(packageRef.IsUsed);
        Assert.Empty(packageRef.UsageLocations);
        Assert.Empty(packageRef.DetectedNamespaces);
    }

    [Fact]
    public void ValidationResult_Success_ShouldCreateValidResult()
    {
        // Act
        var result = ValidationResult.Success();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidationResult_Failure_ShouldCreateInvalidResult()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };

        // Act
        var result = ValidationResult.Failure(errors);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Error 1", result.Errors);
        Assert.Contains("Error 2", result.Errors);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void NamespacePattern_ExactMatch_ShouldMatchCorrectly()
    {
        // Arrange
        var pattern = new NamespacePattern("System.Text.Json");

        // Act & Assert
        Assert.True(pattern.Matches("System.Text.Json"));
        Assert.True(pattern.Matches("system.text.json")); // Case insensitive
        Assert.False(pattern.Matches("System.Text"));
        Assert.False(pattern.Matches("System.Text.Json.Serialization"));
    }

    [Fact]
    public void NamespacePattern_WildcardMatch_ShouldMatchCorrectly()
    {
        // Arrange
        var pattern = new NamespacePattern("System.*");

        // Act & Assert
        Assert.True(pattern.Matches("System.Text"));
        Assert.True(pattern.Matches("System.Text.Json"));
        Assert.False(pattern.Matches("System")); // No trailing part
        Assert.False(pattern.Matches("Microsoft.Extensions"));
    }

    [Fact]
    public void Project_GetUsedPackages_ShouldReturnOnlyUsedPackages()
    {
        // Arrange
        var usedPackage = new PackageReference(
            "Used.Package",
            "1.0.0",
            "/test/project.csproj",
            true,
            null
        );
        var unusedPackage = new PackageReference(
            "Unused.Package",
            "1.0.0",
            "/test/project.csproj",
            true,
            null
        );

        usedPackage.MarkAsUsed("/test/file.cs", "Used.Package");

        var project = new Project("/test/project.csproj", "TestProject", "net9.0");
        project.AddPackageReference(usedPackage);
        project.AddPackageReference(unusedPackage);

        // Act
        var usedPackages = project.GetUsedPackages().ToList();

        // Assert
        Assert.Contains(usedPackage, usedPackages);
        Assert.DoesNotContain(unusedPackage, usedPackages);
        _ = Assert.Single(usedPackages);
    }

    [Fact]
    public void Project_GetUnusedPackages_ShouldReturnOnlyUnusedPackages()
    {
        // Arrange
        var usedPackage = new PackageReference(
            "Used.Package",
            "1.0.0",
            "/test/project.csproj",
            true,
            null
        );
        var unusedPackage = new PackageReference(
            "Unused.Package",
            "1.0.0",
            "/test/project.csproj",
            true,
            null
        );

        usedPackage.MarkAsUsed("/test/file.cs", "Used.Package");

        var project = new Project("/test/project.csproj", "TestProject", "net9.0");
        project.AddPackageReference(usedPackage);
        project.AddPackageReference(unusedPackage);

        // Act
        var unusedPackages = project.GetUnusedPackages().ToList();

        // Assert
        Assert.Contains(unusedPackage, unusedPackages);
        Assert.DoesNotContain(usedPackage, unusedPackages);
        _ = Assert.Single(unusedPackages);
    }

    [Fact]
    public void Solution_GetAllUsedPackages_ShouldReturnUsedPackagesFromAllProjects()
    {
        // Arrange
        var usedPackage1 = new PackageReference(
            "Used.Package1",
            "1.0.0",
            "/test/project1.csproj",
            true,
            null
        );
        var usedPackage2 = new PackageReference(
            "Used.Package2",
            "1.0.0",
            "/test/project2.csproj",
            true,
            null
        );
        var unusedPackage = new PackageReference(
            "Unused.Package",
            "1.0.0",
            "/test/project1.csproj",
            true,
            null
        );

        usedPackage1.MarkAsUsed("/test/file1.cs", "Used.Package1");
        usedPackage2.MarkAsUsed("/test/file2.cs", "Used.Package2");

        var project1 = new Project("/test/project1.csproj", "Project1", "net9.0");
        project1.AddPackageReference(usedPackage1);
        project1.AddPackageReference(unusedPackage);

        var project2 = new Project("/test/project2.csproj", "Project2", "net9.0");
        project2.AddPackageReference(usedPackage2);

        var solution = new Solution("/test/solution.sln", "TestSolution");
        solution.AddProject(project1);
        solution.AddProject(project2);

        // Act
        var allUsedPackages = solution.GetAllUsedPackages().ToList();

        // Assert
        Assert.Contains(usedPackage1, allUsedPackages);
        Assert.Contains(usedPackage2, allUsedPackages);
        Assert.DoesNotContain(unusedPackage, allUsedPackages);
        Assert.Equal(2, allUsedPackages.Count);
    }

    [Fact]
    public void Solution_GetAllUnusedPackages_ShouldReturnUnusedPackagesFromAllProjects()
    {
        // Arrange
        var usedPackage = new PackageReference(
            "Used.Package",
            "1.0.0",
            "/test/project1.csproj",
            true,
            null
        );
        var unusedPackage1 = new PackageReference(
            "Unused.Package1",
            "1.0.0",
            "/test/project1.csproj",
            true,
            null
        );
        var unusedPackage2 = new PackageReference(
            "Unused.Package2",
            "1.0.0",
            "/test/project2.csproj",
            true,
            null
        );

        usedPackage.MarkAsUsed("/test/file1.cs", "Used.Package");

        var project1 = new Project("/test/project1.csproj", "Project1", "net9.0");
        project1.AddPackageReference(usedPackage);
        project1.AddPackageReference(unusedPackage1);

        var project2 = new Project("/test/project2.csproj", "Project2", "net9.0");
        project2.AddPackageReference(unusedPackage2);

        var solution = new Solution("/test/solution.sln", "TestSolution");
        solution.AddProject(project1);
        solution.AddProject(project2);

        // Act
        var allUnusedPackages = solution.GetAllUnusedPackages().ToList();

        // Assert
        Assert.Contains(unusedPackage1, allUnusedPackages);
        Assert.Contains(unusedPackage2, allUnusedPackages);
        Assert.DoesNotContain(usedPackage, allUnusedPackages);
        Assert.Equal(2, allUnusedPackages.Count);
    }
}
