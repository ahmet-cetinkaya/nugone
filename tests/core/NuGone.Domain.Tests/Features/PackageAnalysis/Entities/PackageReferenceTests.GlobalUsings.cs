using FluentAssertions;
using NuGone.Domain.Features.PackageAnalysis.Entities;

namespace NuGone.Domain.Tests.Features.PackageAnalysis.Entities;

/// <summary>
/// Tests for the PackageReference entity's global using functionality.
/// </summary>
public class PackageReferenceGlobalUsingsTests
{
    [Fact]
    public void Constructor_WithGlobalUsing_ShouldSetHasGlobalUsingProperty()
    {
        // Arrange
        var packageId = "Xunit";
        var version = "2.4.2";
        var projectPath = "/path/to/project.csproj";
        var hasGlobalUsing = true;

        // Act
        var packageRef = new PackageReference(
            packageId,
            version,
            projectPath,
            hasGlobalUsing: hasGlobalUsing
        );

        // Assert
        packageRef.PackageId.Should().Be(packageId);
        packageRef.Version.Should().Be(version);
        packageRef.ProjectPath.Should().Be(projectPath);
        packageRef.HasGlobalUsing.Should().BeTrue();
        packageRef.IsDirect.Should().BeTrue(); // Default value
        packageRef.Condition.Should().BeNull(); // Default value
    }

    [Fact]
    public void Constructor_WithoutGlobalUsing_ShouldSetHasGlobalUsingToFalse()
    {
        // Arrange
        var packageId = "Xunit";
        var version = "2.4.2";
        var projectPath = "/path/to/project.csproj";

        // Act
        var packageRef = new PackageReference(packageId, version, projectPath);

        // Assert
        packageRef.HasGlobalUsing.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithAllParameters_ShouldSetAllProperties()
    {
        // Arrange
        var packageId = "Xunit";
        var version = "2.4.2";
        var projectPath = "/path/to/project.csproj";
        var isDirect = false;
        var condition = "'$(Configuration)' == 'Debug'";
        var hasGlobalUsing = true;

        // Act
        var packageRef = new PackageReference(
            packageId,
            version,
            projectPath,
            isDirect,
            condition,
            hasGlobalUsing
        );

        // Assert
        packageRef.PackageId.Should().Be(packageId);
        packageRef.Version.Should().Be(version);
        packageRef.ProjectPath.Should().Be(projectPath);
        packageRef.IsDirect.Should().Be(isDirect);
        packageRef.Condition.Should().Be(condition);
        packageRef.HasGlobalUsing.Should().Be(hasGlobalUsing);
    }

    [Fact]
    public void ToString_WithGlobalUsing_ShouldNotIncludeGlobalUsingInformation()
    {
        // Arrange
        var packageRef = new PackageReference(
            "Xunit",
            "2.4.2",
            "/path/to/project.csproj",
            hasGlobalUsing: true
        );

        // Act
        var result = packageRef.ToString();

        // Assert
        // The ToString method doesn't include global using information by design
        // It focuses on the core package reference information
        result.Should().Be("Xunit 2.4.2 (Direct) - Unused");
    }

    [Fact]
    public void Equals_ShouldNotConsiderGlobalUsingFlag()
    {
        // Arrange
        var packageId = "Xunit";
        var version = "2.4.2";
        var projectPath = "/path/to/project.csproj";

        var packageRef1 = new PackageReference(
            packageId,
            version,
            projectPath,
            hasGlobalUsing: true
        );
        var packageRef2 = new PackageReference(
            packageId,
            version,
            projectPath,
            hasGlobalUsing: false
        );

        // Act & Assert
        // Equality should be based on PackageId, Version, and ProjectPath only
        packageRef1.Equals(packageRef2).Should().BeTrue();
        packageRef1.GetHashCode().Should().Be(packageRef2.GetHashCode());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HasGlobalUsing_ShouldReturnCorrectValue(bool hasGlobalUsing)
    {
        // Arrange
        var packageRef = new PackageReference(
            "Xunit",
            "2.4.2",
            "/path/to/project.csproj",
            hasGlobalUsing: hasGlobalUsing
        );

        // Act & Assert
        packageRef.HasGlobalUsing.Should().Be(hasGlobalUsing);
    }
}
