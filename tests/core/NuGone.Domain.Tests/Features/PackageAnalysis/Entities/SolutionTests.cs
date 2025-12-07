using FluentAssertions;
using NuGone.Domain.Features.PackageAnalysis.Entities;
using Xunit;

namespace NuGone.Domain.Tests.Features.PackageAnalysis.Entities;

/// <summary>
/// Tests for the Solution entity
/// </summary>
public class SolutionTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateSolution()
    {
        // Arrange
        var filePath = "/path/to/solution.slnx";
        var name = "TestSolution";

        // Act
        var solution = new Solution(filePath, name);

        // Assert
        solution.FilePath.Should().Be(filePath);
        solution.Name.Should().Be(name);
        solution.IsVirtual.Should().BeFalse();
        solution.Projects.Should().NotBeNull();
        solution.Projects.Should().BeEmpty();
        solution.CentralPackageManagementEnabled.Should().BeFalse();
        solution.DirectoryPackagesPropsPath.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidFilePath_ShouldThrowArgumentException(string? filePath)
    {
        // Arrange
        var name = "TestSolution";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new Solution(filePath!, name));
        ex.ParamName.Should().Be("filePath");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ShouldThrowArgumentException(string? name)
    {
        // Arrange
        var filePath = "/path/to/solution.slnx";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new Solution(filePath, name!));
        ex.ParamName.Should().Be("name");
    }

    [Fact]
    public void Constructor_WithVirtualFlag_ShouldSetIsVirtual()
    {
        // Arrange
        var filePath = "/virtual/solution";
        var name = "VirtualSolution";

        // Act
        var solution = new Solution(filePath, name, isVirtual: true);

        // Assert
        solution.IsVirtual.Should().BeTrue();
    }

    [Fact]
    public void DirectoryPath_ShouldReturnCorrectDirectory()
    {
        // Arrange
        var filePath = "/path/to/solution.slnx";
        var solution = new Solution(filePath, "TestSolution");

        // Act
        var directoryPath = solution.DirectoryPath;

        // Assert
        directoryPath.Should().Be("/path/to");
    }

    [Fact]
    public void AddProject_WithValidProject_ShouldAddToProjects()
    {
        // Arrange
        var solution = new Solution("/path/to/solution.slnx", "TestSolution");
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");

        // Act
        solution.AddProject(project);

        // Assert
        solution.Projects.Should().Contain(project);
        solution.Projects.Should().HaveCount(1);
    }

    [Fact]
    public void AddProject_WithNullProject_ShouldThrowArgumentNullException()
    {
        // Arrange
        var solution = new Solution("/path/to/solution.slnx", "TestSolution");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => solution.AddProject(null!));
    }

    [Fact]
    public void AddProject_WithDuplicateProject_ShouldNotAddDuplicate()
    {
        // Arrange
        var solution = new Solution("/path/to/solution.slnx", "TestSolution");
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");

        // Act
        solution.AddProject(project);
        solution.AddProject(project); // Add same project again

        // Assert
        solution.Projects.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveProject_WithExistingProject_ShouldRemoveAndReturnTrue()
    {
        // Arrange
        var solution = new Solution("/path/to/solution.slnx", "TestSolution");
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        solution.AddProject(project);

        // Act
        var result = solution.RemoveProject(project);

        // Assert
        result.Should().BeTrue();
        solution.Projects.Should().BeEmpty();
    }

    [Fact]
    public void RemoveProject_WithNonExistentProject_ShouldReturnFalse()
    {
        // Arrange
        var solution = new Solution("/path/to/solution.slnx", "TestSolution");
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");

        // Act
        var result = solution.RemoveProject(project);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RemoveProject_WithNullProject_ShouldThrowArgumentNullException()
    {
        // Arrange
        var solution = new Solution("/path/to/solution.slnx", "TestSolution");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => solution.RemoveProject(null!));
    }

    [Fact]
    public void EnableCentralPackageManagement_WithValidPath_ShouldEnableAndSetPath()
    {
        // Arrange
        var solution = new Solution("/path/to/solution.slnx", "TestSolution");
        var directoryPackagesPropsPath = "/path/to/Directory.Packages.props";

        // Act
        solution.EnableCentralPackageManagement(directoryPackagesPropsPath!);

        // Assert
        solution.CentralPackageManagementEnabled.Should().BeTrue();
        solution.DirectoryPackagesPropsPath.Should().Be(directoryPackagesPropsPath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EnableCentralPackageManagement_WithInvalidPath_ShouldThrowArgumentException(
        string? path
    )
    {
        // Arrange
        var solution = new Solution("/path/to/solution.slnx", "TestSolution");

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            solution.EnableCentralPackageManagement(path!)
        );
        ex.ParamName.Should().Be("directoryPackagesPropsPath");
    }

    [Fact]
    public void DisableCentralPackageManagement_ShouldDisableAndClearPath()
    {
        // Arrange
        var solution = new Solution("/path/to/solution.slnx", "TestSolution");
        solution.EnableCentralPackageManagement("/path/to/Directory.Packages.props");

        // Act
        solution.DisableCentralPackageManagement();

        // Assert
        solution.CentralPackageManagementEnabled.Should().BeFalse();
        solution.DirectoryPackagesPropsPath.Should().BeNull();
    }

    [Fact]
    public void GetAllPackageReferences_ShouldReturnAllPackageReferencesFromAllProjects()
    {
        // Arrange
        var solution = new Solution("/path/to/solution.slnx", "TestSolution");
        var project1 = new Project("/path/to/project1.csproj", "Project1", "net9.0");
        var project2 = new Project("/path/to/project2.csproj", "Project2", "net9.0");

        var package1 = new PackageReference("Package1", "1.0.0", project1.FilePath);
        var package2 = new PackageReference("Package2", "2.0.0", project1.FilePath);
        var package3 = new PackageReference("Package3", "3.0.0", project2.FilePath);

        project1.AddPackageReference(package1);
        project1.AddPackageReference(package2);
        project2.AddPackageReference(package3);

        solution.AddProject(project1);
        solution.AddProject(project2);

        // Act
        var allPackages = solution.GetAllPackageReferences();

        // Assert
        allPackages.Should().HaveCount(3);
        allPackages.Should().Contain(package1);
        allPackages.Should().Contain(package2);
        allPackages.Should().Contain(package3);
    }

    [Fact]
    public void GetAllUnusedPackages_ShouldReturnUnusedPackagesFromAllProjects()
    {
        // Arrange
        var solution = new Solution("/path/to/solution.slnx", "TestSolution");
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");

        var usedPackage = new PackageReference("UsedPackage", "1.0.0", project.FilePath);
        var unusedPackage = new PackageReference("UnusedPackage", "2.0.0", project.FilePath);

        usedPackage.MarkAsUsed("/path/to/file.cs");

        project.AddPackageReference(usedPackage);
        project.AddPackageReference(unusedPackage);
        solution.AddProject(project);

        // Act
        var unusedPackages = solution.GetAllUnusedPackages();

        // Assert
        unusedPackages.Should().HaveCount(1);
        unusedPackages.Should().Contain(unusedPackage);
        unusedPackages.Should().NotContain(usedPackage);
    }

    [Fact]
    public void GetAllUsedPackages_ShouldReturnUsedPackagesFromAllProjects()
    {
        // Arrange
        var solution = new Solution("/path/to/solution.slnx", "TestSolution");
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");

        var usedPackage = new PackageReference("UsedPackage", "1.0.0", project.FilePath);
        var unusedPackage = new PackageReference("UnusedPackage", "2.0.0", project.FilePath);

        usedPackage.MarkAsUsed("/path/to/file.cs");

        project.AddPackageReference(usedPackage);
        project.AddPackageReference(unusedPackage);
        solution.AddProject(project);

        // Act
        var usedPackages = solution.GetAllUsedPackages();

        // Assert
        usedPackages.Should().HaveCount(1);
        usedPackages.Should().Contain(usedPackage);
        usedPackages.Should().NotContain(unusedPackage);
    }

    [Fact]
    public void GetPackageReferencesGroupedById_ShouldGroupPackagesById()
    {
        // Arrange
        var solution = new Solution("/path/to/solution.slnx", "TestSolution");
        var project1 = new Project("/path/to/project1.csproj", "Project1", "net9.0");
        var project2 = new Project("/path/to/project2.csproj", "Project2", "net9.0");

        var package1 = new PackageReference("SamePackage", "1.0.0", project1.FilePath);
        var package2 = new PackageReference("SamePackage", "2.0.0", project2.FilePath);
        var package3 = new PackageReference("DifferentPackage", "3.0.0", project1.FilePath);

        project1.AddPackageReference(package1);
        project2.AddPackageReference(package2);
        project1.AddPackageReference(package3);

        solution.AddProject(project1);
        solution.AddProject(project2);

        // Act
        var groupedPackages = solution.GetPackageReferencesGroupedById();

        // Assert
        groupedPackages.Should().HaveCount(2);
        groupedPackages.Should().ContainKey("SamePackage");
        groupedPackages.Should().ContainKey("DifferentPackage");
        groupedPackages["SamePackage"].Should().HaveCount(2);
        groupedPackages["DifferentPackage"].Should().HaveCount(1);
    }

    [Fact]
    public void GetPackageStatistics_ShouldReturnCorrectStatistics()
    {
        // Arrange
        var solution = new Solution("/path/to/solution.slnx", "TestSolution");
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");

        var usedPackage = new PackageReference("UsedPackage", "1.0.0", project.FilePath);
        var unusedPackage = new PackageReference("UnusedPackage", "2.0.0", project.FilePath);

        usedPackage.MarkAsUsed("/path/to/file.cs");

        project.AddPackageReference(usedPackage);
        project.AddPackageReference(unusedPackage);
        solution.AddProject(project);

        // Act
        var (total, used, unused) = solution.GetPackageStatistics();

        // Assert
        total.Should().Be(2);
        used.Should().Be(1);
        unused.Should().Be(1);
    }

    [Fact]
    public void FindProjectByPath_WithExistingPath_ShouldReturnProject()
    {
        // Arrange
        var solution = new Solution("/path/to/solution.slnx", "TestSolution");
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        solution.AddProject(project);

        // Act
        var found = solution.FindProjectByPath("/path/to/project.csproj");

        // Assert
        found.Should().Be(project);
    }

    [Fact]
    public void FindProjectByPath_WithNonExistentPath_ShouldReturnNull()
    {
        // Arrange
        var solution = new Solution("/path/to/solution.slnx", "TestSolution");
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        solution.AddProject(project);

        // Act
        var found = solution.FindProjectByPath("/path/to/nonexistent.csproj");

        // Assert
        found.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FindProjectByPath_WithInvalidPath_ShouldReturnNull(string? path)
    {
        // Arrange
        var solution = new Solution("/path/to/solution.slnx", "TestSolution");

        // Act
        var result = solution.FindProjectByPath(path!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FindProjectByPath_ShouldBeCaseInsensitive()
    {
        // Arrange
        var solution = new Solution("/path/to/solution.slnx", "TestSolution");
        var project = new Project("/path/to/PROJECT.csproj", "TestProject", "net9.0");
        solution.AddProject(project);

        // Act
        var found = solution.FindProjectByPath("/path/to/project.csproj");

        // Assert
        found.Should().Be(project);
    }

    [Fact]
    public void FindProjectByName_WithExistingName_ShouldReturnProject()
    {
        // Arrange
        var solution = new Solution("/path/to/solution.slnx", "TestSolution");
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        solution.AddProject(project);

        // Act
        var found = solution.FindProjectByName("TestProject");

        // Assert
        found.Should().Be(project);
    }

    [Fact]
    public void FindProjectByName_WithNonExistentName_ShouldReturnNull()
    {
        // Arrange
        var solution = new Solution("/path/to/solution.slnx", "TestSolution");
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        solution.AddProject(project);

        // Act
        var found = solution.FindProjectByName("NonExistent");

        // Assert
        found.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FindProjectByName_WithInvalidName_ShouldReturnNull(string? name)
    {
        // Arrange
        var solution = new Solution("/path/to/solution.slnx", "TestSolution");

        // Act
        var result = solution.FindProjectByName(name!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FindProjectByName_ShouldBeCaseInsensitive()
    {
        // Arrange
        var solution = new Solution("/path/to/solution.slnx", "TestSolution");
        var project = new Project("/path/to/project.csproj", "TestProject", "net9.0");
        solution.AddProject(project);

        // Act
        var found = solution.FindProjectByName("testproject");

        // Assert
        found.Should().Be(project);
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var solution = new Solution("/path/to/solution.slnx", "TestSolution");
        var project1 = new Project("/path/to/project1.csproj", "Project1", "net9.0");
        var project2 = new Project("/path/to/project2.csproj", "Project2", "net9.0");

        var package = new PackageReference("TestPackage", "1.0.0", project1.FilePath);
        project1.AddPackageReference(package);

        solution.AddProject(project1);
        solution.AddProject(project2);

        // Act
        var result = solution.ToString();

        // Assert
        result.Should().Be("TestSolution - 2 projects, 1 packages");
    }

    [Fact]
    public void Equals_WithSameFilePath_ShouldReturnTrue()
    {
        // Arrange
        var solution1 = new Solution("/path/to/solution.slnx", "Solution1");
        var solution2 = new Solution("/path/to/solution.slnx", "Solution2");

        // Act & Assert
        solution1.Equals(solution2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentFilePaths_ShouldReturnFalse()
    {
        // Arrange
        var solution1 = new Solution("/path/to/solution1.slnx", "SameName");
        var solution2 = new Solution("/path/to/solution2.slnx", "SameName");

        // Act & Assert
        solution1.Equals(solution2).Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldBeCaseInsensitive()
    {
        // Arrange
        var solution1 = new Solution("/path/to/SOLUTION.slnx", "Solution1");
        var solution2 = new Solution("/path/to/solution.slnx", "Solution2");

        // Act & Assert
        solution1.Equals(solution2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var solution = new Solution("/path/to/solution.slnx", "TestSolution");

        // Act & Assert
        solution.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentType_ShouldReturnFalse()
    {
        // Arrange
        var solution = new Solution("/path/to/solution.slnx", "TestSolution");

        // Act & Assert
        solution.Equals("string").Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_ShouldBeCaseInsensitive()
    {
        // Arrange
        var solution1 = new Solution("/path/to/SOLUTION.slnx", "Solution1");
        var solution2 = new Solution("/path/to/solution.slnx", "Solution2");

        // Act & Assert
        solution1.GetHashCode().Should().Be(solution2.GetHashCode());
    }
}
