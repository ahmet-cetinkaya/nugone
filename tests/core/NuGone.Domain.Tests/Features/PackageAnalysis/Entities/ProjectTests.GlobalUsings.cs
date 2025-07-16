using FluentAssertions;
using NuGone.Domain.Features.PackageAnalysis.Entities;

namespace NuGone.Domain.Tests.Features.PackageAnalysis.Entities;

/// <summary>
/// Tests for the Project entity's global using functionality.
/// </summary>
public class ProjectGlobalUsingsTests
{
    [Fact]
    public void Constructor_ShouldInitializeEmptyGlobalUsingsCollection()
    {
        // Arrange & Act
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");

        // Assert
        project.GlobalUsings.Should().NotBeNull();
        project.GlobalUsings.Should().BeEmpty();
    }

    [Fact]
    public void AddGlobalUsing_WithValidGlobalUsing_ShouldAddToCollection()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        var globalUsing = new GlobalUsing("Xunit", "/path/to/project.csproj");

        // Act
        project.AddGlobalUsing(globalUsing);

        // Assert
        project.GlobalUsings.Should().HaveCount(1);
        project.GlobalUsings.Should().Contain(globalUsing);
    }

    [Fact]
    public void AddGlobalUsing_WithNullGlobalUsing_ShouldThrowArgumentNullException()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");

        // Act & Assert
        var action = () => project.AddGlobalUsing(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddGlobalUsing_WithDuplicateGlobalUsing_ShouldNotAddDuplicate()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        var globalUsing = new GlobalUsing("Xunit", "/path/to/project.csproj");

        // Act
        project.AddGlobalUsing(globalUsing);
        project.AddGlobalUsing(globalUsing); // Add the same global using again

        // Assert
        project.GlobalUsings.Should().HaveCount(1);
        project.GlobalUsings.Should().Contain(globalUsing);
    }

    [Fact]
    public void RemoveGlobalUsing_WithExistingGlobalUsing_ShouldRemoveAndReturnTrue()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        var globalUsing = new GlobalUsing("Xunit", "/path/to/project.csproj");
        project.AddGlobalUsing(globalUsing);

        // Act
        var result = project.RemoveGlobalUsing(globalUsing);

        // Assert
        result.Should().BeTrue();
        project.GlobalUsings.Should().BeEmpty();
    }

    [Fact]
    public void RemoveGlobalUsing_WithNonExistentGlobalUsing_ShouldReturnFalse()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        var globalUsing = new GlobalUsing("Xunit", "/path/to/project.csproj");

        // Act
        var result = project.RemoveGlobalUsing(globalUsing);

        // Assert
        result.Should().BeFalse();
        project.GlobalUsings.Should().BeEmpty();
    }

    [Fact]
    public void RemoveGlobalUsing_WithNullGlobalUsing_ShouldThrowArgumentNullException()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");

        // Act & Assert
        var action = () => project.RemoveGlobalUsing(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void HasGlobalUsingForPackage_WithExistingPackage_ShouldReturnTrue()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        var globalUsing = new GlobalUsing("Xunit", "/path/to/project.csproj");
        project.AddGlobalUsing(globalUsing);

        // Act
        var result = project.HasGlobalUsingForPackage("Xunit");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasGlobalUsingForPackage_WithNonExistentPackage_ShouldReturnFalse()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        var globalUsing = new GlobalUsing("Xunit", "/path/to/project.csproj");
        project.AddGlobalUsing(globalUsing);

        // Act
        var result = project.HasGlobalUsingForPackage("Moq");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasGlobalUsingForPackage_WithCaseInsensitivePackageId_ShouldReturnTrue()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        var globalUsing = new GlobalUsing("Xunit", "/path/to/project.csproj");
        project.AddGlobalUsing(globalUsing);

        // Act
        var result = project.HasGlobalUsingForPackage("XUNIT");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasGlobalUsingForPackage_WithNullOrEmptyPackageId_ShouldReturnFalse()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        var globalUsing = new GlobalUsing("Xunit", "/path/to/project.csproj");
        project.AddGlobalUsing(globalUsing);

        // Act & Assert
        project.HasGlobalUsingForPackage(null!).Should().BeFalse();
        project.HasGlobalUsingForPackage("").Should().BeFalse();
        project.HasGlobalUsingForPackage("   ").Should().BeFalse();
    }

    [Fact]
    public void AddMultipleGlobalUsings_ShouldMaintainAllUniqueEntries()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        var globalUsing1 = new GlobalUsing("Xunit", "/path/to/project.csproj");
        var globalUsing2 = new GlobalUsing("Moq", "/path/to/project.csproj");
        var globalUsing3 = new GlobalUsing("FluentAssertions", "/path/to/project.csproj");

        // Act
        project.AddGlobalUsing(globalUsing1);
        project.AddGlobalUsing(globalUsing2);
        project.AddGlobalUsing(globalUsing3);

        // Assert
        project.GlobalUsings.Should().HaveCount(3);
        project.GlobalUsings.Should().Contain(globalUsing1);
        project.GlobalUsings.Should().Contain(globalUsing2);
        project.GlobalUsings.Should().Contain(globalUsing3);
    }
}