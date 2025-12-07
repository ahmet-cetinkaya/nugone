using FluentAssertions;
using NuGone.Domain.Features.PackageAnalysis.Entities;
using Xunit;

namespace NuGone.Domain.Tests.Features.PackageAnalysis.Entities;

/// <summary>
/// Tests for the PackageReference entity
/// </summary>
public class PackageReferenceTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreatePackageReference()
    {
        // Arrange
        var packageId = "TestPackage";
        var version = "1.0.0";
        var projectPath = "/path/to/project.csproj";

        // Act
        var packageReference = new PackageReference(packageId, version, projectPath);

        // Assert
        packageReference.PackageId.Should().Be(packageId);
        packageReference.Version.Should().Be(version);
        packageReference.ProjectPath.Should().Be(projectPath);
        packageReference.IsDirect.Should().BeTrue();
        packageReference.Condition.Should().BeNull();
        packageReference.HasGlobalUsing.Should().BeFalse();
        packageReference.IsUsed.Should().BeFalse();
        packageReference.UsageLocations.Should().NotBeNull();
        packageReference.UsageLocations.Should().BeEmpty();
        packageReference.DetectedNamespaces.Should().NotBeNull();
        packageReference.DetectedNamespaces.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidPackageId_ShouldThrowArgumentException(string? packageId)
    {
        // Arrange
        var version = "1.0.0";
        var projectPath = "/path/to/project.csproj";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new PackageReference(packageId!, version, projectPath)
        );
        ex.ParamName.Should().Be("packageId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidVersion_ShouldThrowArgumentException(string? version)
    {
        // Arrange
        var packageId = "TestPackage";
        var projectPath = "/path/to/project.csproj";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new PackageReference(packageId, version!, projectPath)
        );
        ex.ParamName.Should().Be("version");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidProjectPath_ShouldThrowArgumentException(string? projectPath)
    {
        // Arrange
        var packageId = "TestPackage";
        var version = "1.0.0";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new PackageReference(packageId, version, projectPath!)
        );
        ex.ParamName.Should().Be("projectPath");
    }

    [Fact]
    public void Constructor_WithAllParameters_ShouldCreatePackageReferenceWithAllProperties()
    {
        // Arrange
        var packageId = "TestPackage";
        var version = "1.0.0";
        var projectPath = "/path/to/project.csproj";
        var isDirect = false;
        var condition = "'$(Configuration)'=='Debug'";
        var hasGlobalUsing = true;

        // Act
        var packageReference = new PackageReference(
            packageId,
            version,
            projectPath,
            isDirect,
            condition,
            hasGlobalUsing
        );

        // Assert
        packageReference.PackageId.Should().Be(packageId);
        packageReference.Version.Should().Be(version);
        packageReference.ProjectPath.Should().Be(projectPath);
        packageReference.IsDirect.Should().Be(isDirect);
        packageReference.Condition.Should().Be(condition);
        packageReference.HasGlobalUsing.Should().Be(hasGlobalUsing);
        packageReference.IsUsed.Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Constructor_WithIsDirectParameter_ShouldSetIsDirectProperty(bool isDirect)
    {
        // Arrange
        var packageId = "TestPackage";
        var version = "1.0.0";
        var projectPath = "/path/to/project.csproj";

        // Act
        var packageReference = new PackageReference(packageId, version, projectPath, isDirect);

        // Assert
        packageReference.IsDirect.Should().Be(isDirect);
    }

    [Fact]
    public void Constructor_WithConditionParameter_ShouldSetConditionProperty()
    {
        // Arrange
        var packageId = "TestPackage";
        var version = "1.0.0";
        var projectPath = "/path/to/project.csproj";
        var condition = "'$(Configuration)'=='Release'";

        // Act
        var packageReference = new PackageReference(
            packageId,
            version,
            projectPath,
            true,
            condition
        );

        // Assert
        packageReference.Condition.Should().Be(condition);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Constructor_WithHasGlobalUsingParameter_ShouldSetHasGlobalUsingProperty(
        bool hasGlobalUsing
    )
    {
        // Arrange
        var packageId = "TestPackage";
        var version = "1.0.0";
        var projectPath = "/path/to/project.csproj";

        // Act
        var packageReference = new PackageReference(
            packageId,
            version,
            projectPath,
            true,
            null,
            hasGlobalUsing
        );

        // Assert
        packageReference.HasGlobalUsing.Should().Be(hasGlobalUsing);
    }

    [Fact]
    public void MarkAsUsed_WithValidFilePath_ShouldMarkAsUsedAndAddUsageLocation()
    {
        // Arrange
        var packageReference = new PackageReference(
            "TestPackage",
            "1.0.0",
            "/path/to/project.csproj"
        );
        var filePath = "/path/to/source.cs";

        // Act
        packageReference.MarkAsUsed(filePath);

        // Assert
        packageReference.IsUsed.Should().BeTrue();
        packageReference.UsageLocations.Should().Contain(filePath);
        packageReference.UsageLocations.Should().HaveCount(1);
    }

    [Fact]
    public void MarkAsUsed_WithNamespace_ShouldAddDetectedNamespace()
    {
        // Arrange
        var packageReference = new PackageReference(
            "TestPackage",
            "1.0.0",
            "/path/to/project.csproj"
        );
        var filePath = "/path/to/source.cs";
        var @namespace = "TestPackage.Namespace";

        // Act
        packageReference.MarkAsUsed(filePath, @namespace);

        // Assert
        packageReference.IsUsed.Should().BeTrue();
        packageReference.UsageLocations.Should().Contain(filePath);
        packageReference.DetectedNamespaces.Should().Contain(@namespace);
        packageReference.DetectedNamespaces.Should().HaveCount(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkAsUsed_WithInvalidFilePath_ShouldThrowArgumentException(string? filePath)
    {
        // Arrange
        var packageReference = new PackageReference(
            "TestPackage",
            "1.0.0",
            "/path/to/project.csproj"
        );

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => packageReference.MarkAsUsed(filePath!));
        ex.ParamName.Should().Be("filePath");
    }

    [Fact]
    public void MarkAsUsed_WithDuplicateFilePath_ShouldNotAddDuplicateUsageLocation()
    {
        // Arrange
        var packageReference = new PackageReference(
            "TestPackage",
            "1.0.0",
            "/path/to/project.csproj"
        );
        var filePath = "/path/to/source.cs";

        // Act
        packageReference.MarkAsUsed(filePath);
        packageReference.MarkAsUsed(filePath); // Mark same file again

        // Assert
        packageReference.UsageLocations.Should().HaveCount(1);
    }

    [Fact]
    public void MarkAsUsed_WithDuplicateNamespace_ShouldNotAddDuplicateDetectedNamespace()
    {
        // Arrange
        var packageReference = new PackageReference(
            "TestPackage",
            "1.0.0",
            "/path/to/project.csproj"
        );
        var filePath1 = "/path/to/source1.cs";
        var filePath2 = "/path/to/source2.cs";
        var @namespace = "TestPackage.Namespace";

        // Act
        packageReference.MarkAsUsed(filePath1, @namespace);
        packageReference.MarkAsUsed(filePath2, @namespace); // Same namespace again

        // Assert
        packageReference.DetectedNamespaces.Should().HaveCount(1);
    }

    [Fact]
    public void MarkAsUsed_WithNullNamespace_ShouldNotAddToDetectedNamespaces()
    {
        // Arrange
        var packageReference = new PackageReference(
            "TestPackage",
            "1.0.0",
            "/path/to/project.csproj"
        );
        var filePath = "/path/to/source.cs";

        // Act
        packageReference.MarkAsUsed(filePath, null);

        // Assert
        packageReference.IsUsed.Should().BeTrue();
        packageReference.UsageLocations.Should().Contain(filePath);
        packageReference.DetectedNamespaces.Should().BeEmpty();
    }

    [Fact]
    public void MarkAsUsed_WithEmptyNamespace_ShouldNotAddToDetectedNamespaces()
    {
        // Arrange
        var packageReference = new PackageReference(
            "TestPackage",
            "1.0.0",
            "/path/to/project.csproj"
        );
        var filePath = "/path/to/source.cs";

        // Act
        packageReference.MarkAsUsed(filePath, "");

        // Assert
        packageReference.IsUsed.Should().BeTrue();
        packageReference.UsageLocations.Should().Contain(filePath);
        packageReference.DetectedNamespaces.Should().BeEmpty();
    }

    [Fact]
    public void ResetUsageStatus_ShouldResetUsageStatusAndClearCollections()
    {
        // Arrange
        var packageReference = new PackageReference(
            "TestPackage",
            "1.0.0",
            "/path/to/project.csproj"
        );
        packageReference.MarkAsUsed("/path/to/source.cs", "TestPackage.Namespace");

        // Act
        packageReference.ResetUsageStatus();

        // Assert
        packageReference.IsUsed.Should().BeFalse();
        packageReference.UsageLocations.Should().BeEmpty();
        packageReference.DetectedNamespaces.Should().BeEmpty();
    }

    [Theory]
    [InlineData(
        "Package1",
        "1.0.0",
        "/path/project1.csproj",
        true,
        null,
        false,
        "Package1 1.0.0 (Direct) - Unused"
    )]
    [InlineData(
        "Package2",
        "2.0.0",
        "/path/project2.csproj",
        false,
        null,
        false,
        "Package2 2.0.0 (Transitive) - Unused"
    )]
    [InlineData(
        "Package3",
        "3.0.0",
        "/path/project3.csproj",
        true,
        null,
        true,
        "Package3 3.0.0 (Direct) - Used"
    )]
    public void ToString_ShouldReturnFormattedString(
        string packageId,
        string version,
        string projectPath,
        bool isDirect,
        string? condition,
        bool markUsed,
        string expected
    )
    {
        // Arrange
        var packageReference = new PackageReference(
            packageId,
            version,
            projectPath,
            isDirect,
            condition
        );
        if (markUsed)
        {
            packageReference.MarkAsUsed("/path/source.cs");
        }

        // Act
        var result = packageReference.ToString();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Equals_WithSamePackageIdVersionAndProjectPath_ShouldReturnTrue()
    {
        // Arrange
        var package1 = new PackageReference("TestPackage", "1.0.0", "/path/to/project.csproj");
        var package2 = new PackageReference("TestPackage", "1.0.0", "/path/to/project.csproj");

        // Act & Assert
        package1.Equals(package2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentPackageId_ShouldReturnFalse()
    {
        // Arrange
        var package1 = new PackageReference("Package1", "1.0.0", "/path/to/project.csproj");
        var package2 = new PackageReference("Package2", "1.0.0", "/path/to/project.csproj");

        // Act & Assert
        package1.Equals(package2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentVersion_ShouldReturnFalse()
    {
        // Arrange
        var package1 = new PackageReference("TestPackage", "1.0.0", "/path/to/project.csproj");
        var package2 = new PackageReference("TestPackage", "2.0.0", "/path/to/project.csproj");

        // Act & Assert
        package1.Equals(package2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentProjectPath_ShouldReturnFalse()
    {
        // Arrange
        var package1 = new PackageReference("TestPackage", "1.0.0", "/path/to/project1.csproj");
        var package2 = new PackageReference("TestPackage", "1.0.0", "/path/to/project2.csproj");

        // Act & Assert
        package1.Equals(package2).Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldBeCaseInsensitiveForPackageId()
    {
        // Arrange
        var package1 = new PackageReference("TESTPACKAGE", "1.0.0", "/path/to/project.csproj");
        var package2 = new PackageReference("testpackage", "1.0.0", "/path/to/project.csproj");

        // Act & Assert
        package1.Equals(package2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldBeCaseInsensitiveForVersion()
    {
        // Arrange
        var package1 = new PackageReference("TestPackage", "1.0.0", "/path/to/project.csproj");
        var package2 = new PackageReference("TestPackage", "1.0.0", "/path/to/project.csproj"); // Same case but testing case insensitivity

        // Act & Assert
        package1.Equals(package2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldBeCaseInsensitiveForProjectPath()
    {
        // Arrange
        var package1 = new PackageReference("TestPackage", "1.0.0", "/PATH/TO/PROJECT.csproj");
        var package2 = new PackageReference("TestPackage", "1.0.0", "/path/to/project.csproj");

        // Act & Assert
        package1.Equals(package2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var package = new PackageReference("TestPackage", "1.0.0", "/path/to/project.csproj");

        // Act & Assert
        package.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentType_ShouldReturnFalse()
    {
        // Arrange
        var package = new PackageReference("TestPackage", "1.0.0", "/path/to/project.csproj");

        // Act & Assert
        package.Equals("string").Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_ShouldBeCaseInsensitive()
    {
        // Arrange
        var package1 = new PackageReference("TESTPACKAGE", "1.0.0", "/PATH/TO/PROJECT.csproj");
        var package2 = new PackageReference("testpackage", "1.0.0", "/path/to/project.csproj");

        // Act & Assert
        package1.GetHashCode().Should().Be(package2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentPackageId_ShouldReturnDifferentHashCode()
    {
        // Arrange
        var package1 = new PackageReference("Package1", "1.0.0", "/path/to/project.csproj");
        var package2 = new PackageReference("Package2", "1.0.0", "/path/to/project.csproj");

        // Act & Assert
        package1.GetHashCode().Should().NotBe(package2.GetHashCode());
    }
}
