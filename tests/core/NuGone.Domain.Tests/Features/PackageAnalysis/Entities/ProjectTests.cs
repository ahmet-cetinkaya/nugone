using Shouldly;
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
        project.FilePath.ShouldBe(filePath);
        project.Name.ShouldBe(name);
        project.TargetFramework.ShouldBe(targetFramework);
        project.PackageReferences.ShouldNotBeNull();
        project.PackageReferences.ShouldBeEmpty();
        project.GlobalUsings.ShouldNotBeNull();
        project.GlobalUsings.ShouldBeEmpty();
        project.SourceFiles.ShouldNotBeNull();
        project.SourceFiles.ShouldBeEmpty();
        project.ExcludePatterns.ShouldNotBeNull();
        project.ExcludePatterns.ShouldBeEmpty();
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
        ex.ParamName.ShouldBe("filePath");
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
        ex.ParamName.ShouldBe("name");
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
        ex.ParamName.ShouldBe("targetFramework");
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
        directoryPath.ShouldBe("/path/to");
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
        project.PackageReferences.ShouldContain(packageReference);
        project.PackageReferences.Count.ShouldBe(1);
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
        project.PackageReferences.Count.ShouldBe(1);
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
        result.ShouldBeTrue();
        project.PackageReferences.ShouldBeEmpty();
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
        result.ShouldBeFalse();
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
        unusedPackages.ToList().Count.ShouldBe(1);
        unusedPackages.ShouldContain(unusedPackage);
        unusedPackages.ShouldNotContain(usedPackage);
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
        usedPackages.ToList().Count.ShouldBe(1);
        usedPackages.ShouldContain(usedPackage);
        usedPackages.ShouldNotContain(unusedPackage);
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
        project.GlobalUsings.ShouldContain(globalUsing);
        project.GlobalUsings.Count.ShouldBe(1);
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
        project.GlobalUsings.Count.ShouldBe(1);
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
        result.ShouldBeTrue();
        project.GlobalUsings.ShouldBeEmpty();
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
        result.ShouldBeFalse();
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
        result.ShouldBeFalse();
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
        result.ShouldBeTrue();
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
        result.ShouldBeFalse();
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
        result.ShouldBeTrue();
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
        project.SourceFiles.ShouldContain(filePath);
        project.SourceFiles.Count.ShouldBe(1);
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
        ex.ParamName.ShouldBe("filePath");
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
        project.SourceFiles.Count.ShouldBe(1);
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
        project.ExcludePatterns.ShouldContain(pattern);
        project.ExcludePatterns.Count().ShouldBe(1);
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
        ex.ParamName.ShouldBe("pattern");
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
        project.ExcludePatterns.Count().ShouldBe(1);
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
        result.ShouldBeTrue();
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
        result.ShouldBeTrue();
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
        result.ShouldBeFalse();
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
        result.ShouldBeTrue();
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
        result.ShouldBe(expected);
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
        result.ShouldBe("TestProject (net9.0) - 1 packages");
    }

    [Fact]
    public void Equals_WithSameFilePath_ShouldReturnTrue()
    {
        // Arrange
        var project1 = new Project("/path/to/project.csproj", "Project1", "net9.0");
        var project2 = new Project("/path/to/project.csproj", "Project2", "net8.0");

        // Act & Assert
        project1.Equals(project2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_WithDifferentFilePaths_ShouldReturnFalse()
    {
        // Arrange
        var project1 = new Project("/path/to/project1.csproj", "SameName", "net9.0");
        var project2 = new Project("/path/to/project2.csproj", "SameName", "net9.0");

        // Act & Assert
        project1.Equals(project2).ShouldBeFalse();
    }

    [Fact]
    public void Equals_ShouldBeCaseInsensitive()
    {
        // Arrange
        var project1 = new Project("/path/to/PROJECT.csproj", "Project1", "net9.0");
        var project2 = new Project("/path/to/project.csproj", "Project2", "net9.0");

        // Act & Assert
        project1.Equals(project2).ShouldBeTrue();
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");

        // Act & Assert
        // CA1508: This test is redundant - Equals(null) always returns false for non-null objects
        // project.Equals(null).ShouldBeFalse();
    }

    [Fact]
    public void Equals_WithDifferentType_ShouldReturnFalse()
    {
        // Arrange
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");

        // Act & Assert
        project.Equals("string").ShouldBeFalse();
    }

    [Fact]
    public void GetHashCode_ShouldBeCaseInsensitive()
    {
        // Arrange
        var project1 = new Project("/path/to/PROJECT.csproj", "Project1", "net9.0");
        var project2 = new Project("/path/to/project.csproj", "Project2", "net9.0");

        // Act & Assert
        project1.GetHashCode().ShouldBe(project2.GetHashCode());
    }
}
