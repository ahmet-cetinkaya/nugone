using FluentAssertions;
using NuGone.Domain.Features.PackageAnalysis.Entities;
using Xunit;

namespace NuGone.Domain.Tests.Features.PackageAnalysis.Entities;

/// <summary>
/// Tests for the Project entity
/// </summary>
public class ProjectTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateProject()
    {
        // Arrange
        var filePath = "/path/to/project.csproj";
        var name = "TestProject";
        var targetFramework = "net9.0";

        // Act
        var project = new Project(filePath, name, targetFramework);

        // Assert
        project.FilePath.Should().Be(filePath);
        project.Name.Should().Be(name);
        project.TargetFramework.Should().Be(targetFramework);
        project.PackageReferences.Should().NotBeNull();
        project.PackageReferences.Should().BeEmpty();
        project.GlobalUsings.Should().NotBeNull();
        project.GlobalUsings.Should().BeEmpty();
        project.SourceFiles.Should().NotBeNull();
        project.SourceFiles.Should().BeEmpty();
        project.ExcludePatterns.Should().NotBeNull();
        project.ExcludePatterns.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidFilePath_ShouldThrowArgumentException(string? filePath)
    {
        // Arrange
        var name = "TestProject";
        var targetFramework = "net9.0";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new Project(filePath!, name, targetFramework)
        );
        ex.ParamName.Should().Be("filePath");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ShouldThrowArgumentException(string? name)
    {
        // Arrange
        var filePath = "/path/to/project.csproj";
        var targetFramework = "net9.0";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new Project(filePath, name!, targetFramework)
        );
        ex.ParamName.Should().Be("name");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidTargetFramework_ShouldThrowArgumentException(
        string? targetFramework
    )
    {
        // Arrange
        var filePath = "/path/to/project.csproj";
        var name = "TestProject";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new Project(filePath, name, targetFramework!)
        );
        ex.ParamName.Should().Be("targetFramework");
    }

    [Fact]
    public void DirectoryPath_ShouldReturnCorrectDirectory()
    {
        // Arrange
        var filePath = "/path/to/project.csproj";
        var project = new Project(filePath, "TestProject", "net9.0");

        // Act
        var directoryPath = project.DirectoryPath;

        // Assert
        directoryPath.Should().Be("/path/to");
    }

    [Fact]
    public void AddPackageReference_WithValidPackageReference_ShouldAddToPackageReferences()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        var packageReference = new PackageReference("TestPackage", "1.0.0", project.FilePath);

        // Act
        project.AddPackageReference(packageReference);

        // Assert
        project.PackageReferences.Should().Contain(packageReference);
        project.PackageReferences.Should().HaveCount(1);
    }

    [Fact]
    public void AddPackageReference_WithNullPackageReference_ShouldThrowArgumentNullException()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => project.AddPackageReference(null!));
    }

    [Fact]
    public void AddPackageReference_WithDuplicatePackageReference_ShouldNotAddDuplicate()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        var packageReference = new PackageReference("TestPackage", "1.0.0", project.FilePath);

        // Act
        project.AddPackageReference(packageReference);
        project.AddPackageReference(packageReference); // Add same package again

        // Assert
        project.PackageReferences.Should().HaveCount(1);
    }

    [Fact]
    public void RemovePackageReference_WithExistingPackageReference_ShouldRemoveAndReturnTrue()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        var packageReference = new PackageReference("TestPackage", "1.0.0", project.FilePath);
        project.AddPackageReference(packageReference);

        // Act
        var result = project.RemovePackageReference(packageReference);

        // Assert
        result.Should().BeTrue();
        project.PackageReferences.Should().BeEmpty();
    }

    [Fact]
    public void RemovePackageReference_WithNonExistentPackageReference_ShouldReturnFalse()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        var packageReference = new PackageReference("TestPackage", "1.0.0", project.FilePath);

        // Act
        var result = project.RemovePackageReference(packageReference);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RemovePackageReference_WithNullPackageReference_ShouldThrowArgumentNullException()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => project.RemovePackageReference(null!));
    }

    [Fact]
    public void GetUnusedPackages_ShouldReturnUnusedPackageReferences()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");

        var usedPackage = new PackageReference("UsedPackage", "1.0.0", project.FilePath);
        var unusedPackage = new PackageReference("UnusedPackage", "2.0.0", project.FilePath);

        usedPackage.MarkAsUsed("/path/to/file.cs");

        project.AddPackageReference(usedPackage);
        project.AddPackageReference(unusedPackage);

        // Act
        var unusedPackages = project.GetUnusedPackages();

        // Assert
        unusedPackages.Should().HaveCount(1);
        unusedPackages.Should().Contain(unusedPackage);
        unusedPackages.Should().NotContain(usedPackage);
    }

    [Fact]
    public void GetUsedPackages_ShouldReturnUsedPackageReferences()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");

        var usedPackage = new PackageReference("UsedPackage", "1.0.0", project.FilePath);
        var unusedPackage = new PackageReference("UnusedPackage", "2.0.0", project.FilePath);

        usedPackage.MarkAsUsed("/path/to/file.cs");

        project.AddPackageReference(usedPackage);
        project.AddPackageReference(unusedPackage);

        // Act
        var usedPackages = project.GetUsedPackages();

        // Assert
        usedPackages.Should().HaveCount(1);
        usedPackages.Should().Contain(usedPackage);
        usedPackages.Should().NotContain(unusedPackage);
    }

    [Fact]
    public void AddGlobalUsing_WithValidGlobalUsing_ShouldAddToGlobalUsings()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        var globalUsing = new GlobalUsing("TestPackage", "/path/to/project.csproj");

        // Act
        project.AddGlobalUsing(globalUsing);

        // Assert
        project.GlobalUsings.Should().Contain(globalUsing);
        project.GlobalUsings.Should().HaveCount(1);
    }

    [Fact]
    public void AddGlobalUsing_WithNullGlobalUsing_ShouldThrowArgumentNullException()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => project.AddGlobalUsing(null!));
    }

    [Fact]
    public void AddGlobalUsing_WithDuplicateGlobalUsing_ShouldNotAddDuplicate()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        var globalUsing = new GlobalUsing("TestPackage", "/path/to/project.csproj");

        // Act
        project.AddGlobalUsing(globalUsing);
        project.AddGlobalUsing(globalUsing); // Add same global using again

        // Assert
        project.GlobalUsings.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveGlobalUsing_WithExistingGlobalUsing_ShouldRemoveAndReturnTrue()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        var globalUsing = new GlobalUsing("TestPackage", "/path/to/project.csproj");
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
        var globalUsing = new GlobalUsing("TestPackage", "/path/to/project.csproj");

        // Act
        var result = project.RemoveGlobalUsing(globalUsing);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RemoveGlobalUsing_WithNullGlobalUsing_ShouldThrowArgumentNullException()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => project.RemoveGlobalUsing(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void HasGlobalUsingForPackage_WithInvalidPackageId_ShouldReturnFalse(string? packageId)
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");

        // Act
        var result = project.HasGlobalUsingForPackage(packageId!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasGlobalUsingForPackage_WithExistingGlobalUsing_ShouldReturnTrue()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        var globalUsing = new GlobalUsing("TestPackage", "/path/to/project.csproj");
        project.AddGlobalUsing(globalUsing);

        // Act
        var result = project.HasGlobalUsingForPackage("TestPackage");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasGlobalUsingForPackage_WithNonExistentGlobalUsing_ShouldReturnFalse()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        var globalUsing = new GlobalUsing("OtherPackage", "/path/to/project.csproj");
        project.AddGlobalUsing(globalUsing);

        // Act
        var result = project.HasGlobalUsingForPackage("TestPackage");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasGlobalUsingForPackage_ShouldBeCaseInsensitive()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        var globalUsing = new GlobalUsing("TestPackage", "/path/to/project.csproj");
        project.AddGlobalUsing(globalUsing);

        // Act
        var result = project.HasGlobalUsingForPackage("testpackage");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void AddSourceFile_WithValidFilePath_ShouldAddToSourceFiles()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        var filePath = "/path/to/source.cs";

        // Act
        project.AddSourceFile(filePath);

        // Assert
        project.SourceFiles.Should().Contain(filePath);
        project.SourceFiles.Should().HaveCount(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddSourceFile_WithInvalidFilePath_ShouldThrowArgumentException(string? filePath)
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => project.AddSourceFile(filePath!));
        ex.ParamName.Should().Be("filePath");
    }

    [Fact]
    public void AddSourceFile_WithDuplicateFilePath_ShouldNotAddDuplicate()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        var filePath = "/path/to/source.cs";

        // Act
        project.AddSourceFile(filePath);
        project.AddSourceFile(filePath); // Add same file again

        // Assert
        project.SourceFiles.Should().HaveCount(1);
    }

    [Fact]
    public void AddExcludePattern_WithValidPattern_ShouldAddToExcludePatterns()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        var pattern = "**/Generated/**";

        // Act
        project.AddExcludePattern(pattern);

        // Assert
        project.ExcludePatterns.Should().Contain(pattern);
        project.ExcludePatterns.Should().HaveCount(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddExcludePattern_WithInvalidPattern_ShouldThrowArgumentException(string? pattern)
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => project.AddExcludePattern(pattern!));
        ex.ParamName.Should().Be("pattern");
    }

    [Fact]
    public void AddExcludePattern_WithDuplicatePattern_ShouldNotAddDuplicate()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        var pattern = "**/Generated/**";

        // Act
        project.AddExcludePattern(pattern);
        project.AddExcludePattern(pattern); // Add same pattern again

        // Assert
        project.ExcludePatterns.Should().HaveCount(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ShouldExcludeFile_WithInvalidFilePath_ShouldReturnTrue(string? filePath)
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");

        // Act
        var result = project.ShouldExcludeFile(filePath!);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldExcludeFile_WithMatchingExcludePattern_ShouldReturnTrue()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        project.AddExcludePattern("**/Generated/**");

        // Act
        var result = project.ShouldExcludeFile("/path/to/Generated/File.cs");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldExcludeFile_WithNonMatchingExcludePattern_ShouldReturnFalse()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        project.AddExcludePattern("**/Generated/**");

        // Act
        var result = project.ShouldExcludeFile("/path/to/Source/File.cs");

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("File.Designer.cs")]
    [InlineData("File.g.cs")]
    [InlineData("File.g.i.cs")]
    [InlineData("file.designer.cs")] // Test case insensitivity
    [InlineData("FILE.G.CS")] // Test case insensitivity
    public void ShouldExcludeFile_WithAutoGeneratedFile_ShouldReturnTrue(string fileName)
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        var filePath = $"/path/to/{fileName}";

        // Act
        var result = project.ShouldExcludeFile(filePath);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("**/Test/**", "/path/to/Test/File.cs", true)]
    [InlineData("**/Test/**", "/path/to/Source/File.cs", false)]
    [InlineData("Generated/*", "/path/to/Generated/File.cs", true)]
    [InlineData("Generated/*", "/path/to/Source/File.cs", false)]
    [InlineData("*Temp*", "/path/to/TempFile.cs", true)]
    [InlineData("*Temp*", "/path/to/Source.cs", false)]
    public void ShouldExcludeFile_WithVariousPatterns_ShouldReturnExpected(
        string pattern,
        string filePath,
        bool expected
    )
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        project.AddExcludePattern(pattern);

        // Act
        var result = project.ShouldExcludeFile(filePath);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        var package = new PackageReference("TestPackage", "1.0.0", project.FilePath);
        project.AddPackageReference(package);

        // Act
        var result = project.ToString();

        // Assert
        result.Should().Be("TestProject (net9.0) - 1 packages");
    }

    [Fact]
    public void Equals_WithSameFilePath_ShouldReturnTrue()
    {
        // Arrange
        var project1 = new Project("/path/to/project.csproj", "Project1", "net9.0");
        var project2 = new Project("/path/to/project.csproj", "Project2", "net8.0");

        // Act & Assert
        project1.Equals(project2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentFilePaths_ShouldReturnFalse()
    {
        // Arrange
        var project1 = new Project("/path/to/project1.csproj", "SameName", "net9.0");
        var project2 = new Project("/path/to/project2.csproj", "SameName", "net9.0");

        // Act & Assert
        project1.Equals(project2).Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldBeCaseInsensitive()
    {
        // Arrange
        var project1 = new Project("/path/to/PROJECT.csproj", "Project1", "net9.0");
        var project2 = new Project("/path/to/project.csproj", "Project2", "net9.0");

        // Act & Assert
        project1.Equals(project2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");

        // Act & Assert
        // CA1508: This test is redundant - Equals(null) always returns false for non-null objects
        // project.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentType_ShouldReturnFalse()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");

        // Act & Assert
        project.Equals("string").Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_ShouldBeCaseInsensitive()
    {
        // Arrange
        var project1 = new Project("/path/to/PROJECT.csproj", "Project1", "net9.0");
        var project2 = new Project("/path/to/project.csproj", "Project2", "net9.0");

        // Act & Assert
        project1.GetHashCode().Should().Be(project2.GetHashCode());
    }
}
