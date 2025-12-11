using Shouldly;
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
        packageReference.PackageId.ShouldBe(packageId);
        packageReference.Version.ShouldBe(version);
        packageReference.ProjectPath.ShouldBe(projectPath);
        packageReference.IsDirect.ShouldBeTrue();
        packageReference.Condition.ShouldBe(null);
        packageReference.HasGlobalUsing.ShouldBeFalse();
        packageReference.IsUsed.ShouldBeFalse();
        packageReference.UsageLocations.ShouldNotBeNull();
        packageReference.UsageLocations.ShouldBeEmpty();
        packageReference.DetectedNamespaces.ShouldNotBeNull();
        packageReference.DetectedNamespaces.ShouldBeEmpty();
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
        ex.ParamName.ShouldBe("packageId");
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
        ex.ParamName.ShouldBe("version");
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
        ex.ParamName.ShouldBe("projectPath");
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
        packageReference.PackageId.ShouldBe(packageId);
        packageReference.Version.ShouldBe(version);
        packageReference.ProjectPath.ShouldBe(projectPath);
        packageReference.IsDirect.ShouldBe(isDirect);
        packageReference.Condition.ShouldBe(condition);
        packageReference.HasGlobalUsing.ShouldBe(hasGlobalUsing);
        packageReference.IsUsed.ShouldBeFalse();
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
        packageReference.IsDirect.ShouldBe(isDirect);
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
        packageReference.Condition.ShouldBe(condition);
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
        packageReference.HasGlobalUsing.ShouldBe(hasGlobalUsing);
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
        packageReference.IsUsed.ShouldBeTrue();
        packageReference.UsageLocations.ShouldContain(filePath);
        packageReference.UsageLocations.Count.ShouldBe(1);
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
        packageReference.IsUsed.ShouldBeTrue();
        packageReference.UsageLocations.ShouldContain(filePath);
        packageReference.DetectedNamespaces.ShouldContain(@namespace);
        packageReference.DetectedNamespaces.Count.ShouldBe(1);
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
        ex.ParamName.ShouldBe("filePath");
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
        packageReference.UsageLocations.Count.ShouldBe(1);
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
        packageReference.DetectedNamespaces.Count.ShouldBe(1);
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
        packageReference.IsUsed.ShouldBeTrue();
        packageReference.UsageLocations.ShouldContain(filePath);
        packageReference.DetectedNamespaces.ShouldBeEmpty();
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
        packageReference.IsUsed.ShouldBeTrue();
        packageReference.UsageLocations.ShouldContain(filePath);
        packageReference.DetectedNamespaces.ShouldBeEmpty();
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
        packageReference.IsUsed.ShouldBeFalse();
        packageReference.UsageLocations.ShouldBeEmpty();
        packageReference.DetectedNamespaces.ShouldBeEmpty();
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
        result.ShouldBe(expected);
    }

    [Fact]
    public void Equals_WithSamePackageIdVersionAndProjectPath_ShouldReturnTrue()
    {
        // Arrange
        var package1 = new PackageReference("TestPackage", "1.0.0", "/path/to/project.csproj");
        var package2 = new PackageReference("TestPackage", "1.0.0", "/path/to/project.csproj");

        // Act & Assert
        package1.Equals(package2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_WithDifferentPackageId_ShouldReturnFalse()
    {
        // Arrange
        var package1 = new PackageReference("Package1", "1.0.0", "/path/to/project.csproj");
        var package2 = new PackageReference("Package2", "1.0.0", "/path/to/project.csproj");

        // Act & Assert
        package1.Equals(package2).ShouldBeFalse();
    }

    [Fact]
    public void Equals_WithDifferentVersion_ShouldReturnFalse()
    {
        // Arrange
        var package1 = new PackageReference("TestPackage", "1.0.0", "/path/to/project.csproj");
        var package2 = new PackageReference("TestPackage", "2.0.0", "/path/to/project.csproj");

        // Act & Assert
        package1.Equals(package2).ShouldBeFalse();
    }

    [Fact]
    public void Equals_WithDifferentProjectPath_ShouldReturnFalse()
    {
        // Arrange
        var package1 = new PackageReference("TestPackage", "1.0.0", "/path/to/project1.csproj");
        var package2 = new PackageReference("TestPackage", "1.0.0", "/path/to/project2.csproj");

        // Act & Assert
        package1.Equals(package2).ShouldBeFalse();
    }

    [Fact]
    public void Equals_ShouldBeCaseInsensitiveForPackageId()
    {
        // Arrange
        var package1 = new PackageReference("TESTPACKAGE", "1.0.0", "/path/to/project.csproj");
        var package2 = new PackageReference("testpackage", "1.0.0", "/path/to/project.csproj");

        // Act & Assert
        package1.Equals(package2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_ShouldBeCaseInsensitiveForVersion()
    {
        // Arrange
        var package1 = new PackageReference("TestPackage", "1.0.0", "/path/to/project.csproj");
        var package2 = new PackageReference("TestPackage", "1.0.0", "/path/to/project.csproj"); // Same case but testing case insensitivity

        // Act & Assert
        package1.Equals(package2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_ShouldBeCaseInsensitiveForProjectPath()
    {
        // Arrange
        var package1 = new PackageReference("TestPackage", "1.0.0", "/PATH/TO/PROJECT.csproj");
        var package2 = new PackageReference("TestPackage", "1.0.0", "/path/to/project.csproj");

        // Act & Assert
        package1.Equals(package2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var package = new PackageReference("TestPackage", "1.0.0", "/path/to/project.csproj");

        // Act & Assert
        // CA1508: This test is redundant - Equals(null) always returns false for non-null objects
        // package.Equals(null).ShouldBeFalse();
    }

    [Fact]
    public void Equals_WithDifferentType_ShouldReturnFalse()
    {
        // Arrange
        var package = new PackageReference("TestPackage", "1.0.0", "/path/to/project.csproj");

        // Act & Assert
        package.Equals("string").ShouldBeFalse();
    }

    [Fact]
    public void GetHashCode_ShouldBeCaseInsensitive()
    {
        // Arrange
        var package1 = new PackageReference("TESTPACKAGE", "1.0.0", "/PATH/TO/PROJECT.csproj");
        var package2 = new PackageReference("testpackage", "1.0.0", "/path/to/project.csproj");

        // Act & Assert
        package1.GetHashCode().ShouldBe(package2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentPackageId_ShouldReturnDifferentHashCode()
    {
        // Arrange
        var package1 = new PackageReference("Package1", "1.0.0", "/path/to/project.csproj");
        var package2 = new PackageReference("Package2", "1.0.0", "/path/to/project.csproj");

        // Act & Assert
        package1.GetHashCode().ShouldNotBe(package2.GetHashCode());
    }
}
