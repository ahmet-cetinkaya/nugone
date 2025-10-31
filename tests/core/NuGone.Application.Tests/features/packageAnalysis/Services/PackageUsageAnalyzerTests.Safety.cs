using Microsoft.Extensions.Logging;
using Moq;
using NuGone.Domain.Features.PackageAnalysis.Entities;
using Shouldly;
using Xunit;

namespace NuGone.Application.Tests.Features.PackageAnalysis.Services;

/// <summary>
/// Tests for RFC-0004 safety mechanism validation.
/// Validates input validation, package name sanitization, project path validation,
/// and integration points with the detection algorithm.
/// </summary>
public partial class PackageUsageAnalyzerTests
{
    [Fact]
    public async Task ValidateInputsAsync_WithValidSolution_ShouldReturnSuccess()
    {
        // Arrange - RFC-0004: Input validation for paths and configuration
        var project = CreateTestProject("ValidProject", "net9.0");
        var solution = CreateTestSolution("ValidSolution", project);

        SetupMockPathExists(solution.FilePath, project.FilePath, project.DirectoryPath);

        // Act
        var result = await _analyzer.ValidateInputsAsync(solution);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task ValidateInputsAsync_WithNullSolution_ShouldReturnFailure()
    {
        // Arrange - RFC-0004: Validate and sanitize all CLI arguments and config file inputs
        Solution? nullSolution = null;

        // Act
        var result = await _analyzer.ValidateInputsAsync(nullSolution!);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain("Solution cannot be null");
    }

    [Fact]
    public async Task ValidateInputsAsync_WithNonExistentSolutionFile_ShouldReturnFailure()
    {
        // Arrange - RFC-0004: Validate package names and project paths before removal
        var project = CreateTestProject("TestProject", "net9.0");
        var solution = CreateTestSolution("NonExistentSolution", project);

        _ = _mockProjectRepository.Setup(r => r.ExistsAsync(solution.FilePath)).ReturnsAsync(false);
        SetupMockPathExists(project.FilePath, project.DirectoryPath);

        // Act
        var result = await _analyzer.ValidateInputsAsync(solution);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain($"Solution file does not exist: {solution.FilePath}");
    }

    [Fact]
    public async Task ValidateInputsAsync_WithNonExistentProjectFile_ShouldReturnFailure()
    {
        // Arrange - RFC-0004: Validate package names and project paths before removal
        var project = CreateTestProject("NonExistentProject", "net9.0");
        var solution = CreateTestSolution("TestSolution", project);

        SetupMockPathExists(solution.FilePath, project.DirectoryPath);
        _ = _mockProjectRepository.Setup(r => r.ExistsAsync(project.FilePath)).ReturnsAsync(false);

        // Act
        var result = await _analyzer.ValidateInputsAsync(solution);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain($"Project file does not exist: {project.FilePath}");
    }

    [Fact]
    public async Task ValidateInputsAsync_WithNonExistentProjectDirectory_ShouldReturnFailure()
    {
        // Arrange - RFC-0004: Validate package names and project paths before removal
        var project = CreateTestProject("TestProject", "net9.0");
        var solution = CreateTestSolution("TestSolution", project);

        SetupMockPathExists(solution.FilePath, project.FilePath);
        _ = _mockProjectRepository
            .Setup(r => r.ExistsAsync(project.DirectoryPath))
            .ReturnsAsync(false);

        // Act
        var result = await _analyzer.ValidateInputsAsync(solution);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain($"Project directory does not exist: {project.DirectoryPath}");
    }

    [Fact]
    public async Task ValidateInputsAsync_WithMultipleValidationErrors_ShouldReturnAllErrors()
    {
        // Arrange - RFC-0004: All errors are reported with clear messages
        var project1 = CreateTestProject("Project1", "net9.0");
        var project2 = CreateTestProject("Project2", "net9.0");
        var solution = CreateTestSolution("TestSolution", project1, project2);

        // Setup all paths as non-existent
        _ = _mockProjectRepository
            .Setup(r => r.ExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var result = await _analyzer.ValidateInputsAsync(solution);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(5); // Solution + 2 projects + 2 directories
        result.Errors.ShouldContain($"Solution file does not exist: {solution.FilePath}");
        result.Errors.ShouldContain($"Project file does not exist: {project1.FilePath}");
        result.Errors.ShouldContain($"Project directory does not exist: {project1.DirectoryPath}");
        result.Errors.ShouldContain($"Project file does not exist: {project2.FilePath}");
        result.Errors.ShouldContain($"Project directory does not exist: {project2.DirectoryPath}");
    }

    [Fact]
    public async Task ScanSourceFilesForUsageAsync_WithValidInputs_ShouldProcessAllFiles()
    {
        // Arrange - RFC-0004: Input validation for file scanning
        var sourceFiles = new[] { "/test/File1.cs", "/test/File2.cs" };
        var packageNamespaces = new[] { "Test.Namespace" };
        var excludePatterns = Array.Empty<string>();

        SetupMockFileContent("/test/File1.cs", "using Test.Namespace;\nvar obj = new TestClass();");
        SetupMockFileContent("/test/File2.cs", "using System;\nConsole.WriteLine(\"Hello\");");

        // Act
        var result = await _analyzer.ScanSourceFilesForUsageAsync(
            sourceFiles,
            packageNamespaces,
            excludePatterns
        );

        // Assert
        result.ShouldContainKey("Test.Namespace");
        result["Test.Namespace"].ShouldContain("/test/File1.cs");
        result["Test.Namespace"].ShouldNotContain("/test/File2.cs");
    }

    [Fact]
    public async Task ScanSourceFilesForUsageAsync_WithExcludePatterns_ShouldSkipMatchingFiles()
    {
        // Arrange - RFC-0002: Exclude files/folders by user-defined patterns
        var sourceFiles = new[] { "/test/Generated/File1.cs", "/test/Normal/File2.cs" };
        var packageNamespaces = new[] { "Test.Namespace" };
        var project = CreateTestProject("TestProject", "net9.0");
        project.AddExcludePattern("**/Generated/**");

        SetupMockFileContent(
            "/test/Generated/File1.cs",
            "using Test.Namespace;\nvar obj = new TestClass();"
        );
        SetupMockFileContent(
            "/test/Normal/File2.cs",
            "using Test.Namespace;\nvar obj = new TestClass();"
        );

        // Act
        var result = await _analyzer.ScanSourceFilesForUsageAsync(
            sourceFiles,
            packageNamespaces,
            project
        );

        // Assert
        result.ShouldContainKey("Test.Namespace");
        result["Test.Namespace"].ShouldNotContain("/test/Generated/File1.cs");
        result["Test.Namespace"].ShouldContain("/test/Normal/File2.cs");
    }

    [Fact]
    public async Task ScanSourceFilesForUsageAsync_WithFileReadError_ShouldLogWarningAndContinue()
    {
        // Arrange - RFC-0004: Failures do not leave the project in a broken state
        var sourceFiles = new[] { "/test/ErrorFile.cs", "/test/GoodFile.cs" };
        var packageNamespaces = new[] { "Test.Namespace" };
        var excludePatterns = Array.Empty<string>();

        _ = _mockProjectRepository
            .Setup(r => r.ReadSourceFileAsync("/test/ErrorFile.cs", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("File access error"));
        SetupMockFileContent(
            "/test/GoodFile.cs",
            "using Test.Namespace;\nvar obj = new TestClass();"
        );

        // Act
        var result = await _analyzer.ScanSourceFilesForUsageAsync(
            sourceFiles,
            packageNamespaces,
            excludePatterns
        );

        // Assert
        result.ShouldContainKey("Test.Namespace");
        result["Test.Namespace"].ShouldContain("/test/GoodFile.cs");
        result["Test.Namespace"].ShouldNotContain("/test/ErrorFile.cs");

        // Verify warning was logged
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) =>
                            v.ToString()!
                                .Contains(
                                    "Error scanning file for namespace usage: /test/ErrorFile.cs"
                                )
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task GetPackageNamespacesAsync_WithValidPackage_ShouldReturnNamespaces()
    {
        // Arrange - RFC-0004: Validate package names before processing
        var packageId = "Valid.Package";
        var version = "1.0.0";
        var targetFramework = "net9.0";
        var expectedNamespaces = new[] { "Valid.Package", "Valid.Package.Extensions" };

        SetupMockPackageNamespaces(packageId, version, targetFramework, expectedNamespaces);

        // Act
        var result = await _analyzer.GetPackageNamespacesAsync(packageId, version, targetFramework);

        // Assert
        result.ShouldBe(expectedNamespaces);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GetPackageNamespacesAsync_WithInvalidPackageId_ShouldHandleGracefully(
        string? invalidPackageId
    )
    {
        // Arrange - RFC-0004: Sanitize all user input
        var version = "1.0.0";
        var targetFramework = "net9.0";

        _ = _mockNuGetRepository
            .Setup(r =>
                r.GetPackageNamespacesAsync(
                    invalidPackageId!,
                    version,
                    targetFramework,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([]);

        // Act
        var result = await _analyzer.GetPackageNamespacesAsync(
            invalidPackageId!,
            version,
            targetFramework
        );

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnalyzeProjectPackageUsageAsync_WithSafetyValidation_ShouldEnsureDataIntegrity()
    {
        // Arrange - RFC-0004: Ensure analysis doesn't leave project in broken state
        var packageRef = CreateTestPackageReference("Test.Package", "1.0.0");
        var project = CreateTestProject("TestProject", "net9.0", packageRef);

        SetupMockSourceFiles(project, "/test/Program.cs");
        SetupMockFileContent("/test/Program.cs", "using Test.Package;\nvar obj = new TestClass();");
        SetupMockPackageNamespaces("Test.Package", "1.0.0", "net9.0", "Test.Package");

        // Verify initial state
        packageRef.IsUsed.ShouldBeFalse();
        packageRef.UsageLocations.ShouldBeEmpty();

        // Act
        await _analyzer.AnalyzeProjectPackageUsageAsync(project);

        // Assert - RFC-0004: Analysis should maintain data integrity
        packageRef.IsUsed.ShouldBeTrue();
        packageRef.UsageLocations.ShouldContain("/test/Program.cs");
        packageRef.DetectedNamespaces.ShouldContain("Test.Package");

        // Verify project state is consistent
        project.GetUsedPackages().ShouldContain(packageRef);
        project.GetUnusedPackages().ShouldNotContain(packageRef);
    }
}
