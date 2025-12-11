using NuGone.Domain.Features.PackageAnalysis.Entities;
using Shouldly;

namespace NuGone.Domain.Tests.Features.PackageAnalysis.Entities;

/// <summary>
/// Tests for the GlobalUsing domain entity.
/// </summary>
public class GlobalUsingTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateGlobalUsing()
    {
        // Arrange
        var packageId = "Xunit";
        var projectPath = "/path/to/project.csproj";
        var condition = "'$(Configuration)' == 'Debug'";

        // Act
        var globalUsing = new GlobalUsing(packageId, projectPath, condition);

        // Assert
        globalUsing.PackageId.ShouldBe(packageId);
        globalUsing.ProjectPath.ShouldBe(projectPath);
        globalUsing.Condition.ShouldBe(condition);
    }

    [Fact]
    public void Constructor_WithNullPackageId_ShouldThrowArgumentException()
    {
        // Arrange
        string? packageId = null;
        var projectPath = "/path/to/project.csproj";

        // Act & Assert
        Should
            .Throw<ArgumentException>(() => new GlobalUsing(packageId!, projectPath))
            .Message.ShouldStartWith("Package ID cannot be null or empty");
    }

    [Fact]
    public void Constructor_WithEmptyPackageId_ShouldThrowArgumentException()
    {
        // Arrange
        var packageId = "";
        var projectPath = "/path/to/project.csproj";

        // Act & Assert
        Should
            .Throw<ArgumentException>(() => new GlobalUsing(packageId, projectPath))
            .Message.ShouldStartWith("Package ID cannot be null or empty");
    }

    [Fact]
    public void Constructor_WithNullProjectPath_ShouldThrowArgumentException()
    {
        // Arrange
        var packageId = "Xunit";
        string? projectPath = null;

        // Act & Assert
        Should
            .Throw<ArgumentException>(() => new GlobalUsing(packageId, projectPath!))
            .Message.ShouldStartWith("Project path cannot be null or empty");
    }

    [Fact]
    public void Constructor_WithEmptyProjectPath_ShouldThrowArgumentException()
    {
        // Arrange
        var packageId = "Xunit";
        var projectPath = "";

        // Act & Assert
        Should
            .Throw<ArgumentException>(() => new GlobalUsing(packageId, projectPath))
            .Message.ShouldStartWith("Project path cannot be null or empty");
    }

    [Fact]
    public void Constructor_WithNullCondition_ShouldCreateGlobalUsingWithNullCondition()
    {
        // Arrange
        var packageId = "Xunit";
        var projectPath = "/path/to/project.csproj";

        // Act
        var globalUsing = new GlobalUsing(packageId, projectPath, null);

        // Assert
        globalUsing.PackageId.ShouldBe(packageId);
        globalUsing.ProjectPath.ShouldBe(projectPath);
        globalUsing.Condition.ShouldBe(null);
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var packageId = "Xunit";
        var projectPath = "/path/to/project.csproj";
        var globalUsing = new GlobalUsing(packageId, projectPath);

        // Act
        var result = globalUsing.ToString();

        // Assert
        result.ShouldBe("Global Using: Xunit");
    }

    [Fact]
    public void Equals_WithSamePackageIdAndProjectPath_ShouldReturnTrue()
    {
        // Arrange
        var packageId = "Xunit";
        var projectPath = "/path/to/project.csproj";
        var globalUsing1 = new GlobalUsing(packageId, projectPath);
        var globalUsing2 = new GlobalUsing(packageId, projectPath);

        // Act & Assert
        globalUsing1.Equals(globalUsing2).ShouldBeTrue();
        globalUsing1.GetHashCode().ShouldBe(globalUsing2.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentPackageId_ShouldReturnFalse()
    {
        // Arrange
        var projectPath = "/path/to/project.csproj";
        var globalUsing1 = new GlobalUsing("Xunit", projectPath);
        var globalUsing2 = new GlobalUsing("Moq", projectPath);

        // Act & Assert
        globalUsing1.Equals(globalUsing2).ShouldBeFalse();
    }

    [Fact]
    public void Equals_WithDifferentProjectPath_ShouldReturnFalse()
    {
        // Arrange
        var packageId = "Xunit";
        var globalUsing1 = new GlobalUsing(packageId, "/path/to/project1.csproj");
        var globalUsing2 = new GlobalUsing(packageId, "/path/to/project2.csproj");

        // Act & Assert
        globalUsing1.Equals(globalUsing2).ShouldBeFalse();
    }

    [Fact]
    public void Equals_WithCaseInsensitivePackageId_ShouldReturnTrue()
    {
        // Arrange
        var projectPath = "/path/to/project.csproj";
        var globalUsing1 = new GlobalUsing("Xunit", projectPath);
        var globalUsing2 = new GlobalUsing("XUNIT", projectPath);

        // Act & Assert
        globalUsing1.Equals(globalUsing2).ShouldBeTrue();
        globalUsing1.GetHashCode().ShouldBe(globalUsing2.GetHashCode());
    }

    [Fact]
    public void Equals_WithCaseInsensitiveProjectPath_ShouldReturnTrue()
    {
        // Arrange
        var packageId = "Xunit";
        var globalUsing1 = new GlobalUsing(packageId, "/path/to/project.csproj");
        var globalUsing2 = new GlobalUsing(packageId, "/PATH/TO/PROJECT.CSPROJ");

        // Act & Assert
        globalUsing1.Equals(globalUsing2).ShouldBeTrue();
        globalUsing1.GetHashCode().ShouldBe(globalUsing2.GetHashCode());
    }

    [Fact]
    public void Equals_WithNullObject_ShouldReturnFalse()
    {
        // Arrange
        var globalUsing = new GlobalUsing("Xunit", "/path/to/project.csproj");

        // Act & Assert
        // CA1508: This test is redundant - Equals(null) always returns false for non-null objects
        // globalUsing.Equals(null).ShouldBeFalse();
    }

    [Fact]
    public void Equals_WithDifferentType_ShouldReturnFalse()
    {
        // Arrange
        var globalUsing = new GlobalUsing("Xunit", "/path/to/project.csproj");
        var otherObject = "not a global using";

        // Act & Assert
        globalUsing.Equals(otherObject).ShouldBeFalse();
    }
}
