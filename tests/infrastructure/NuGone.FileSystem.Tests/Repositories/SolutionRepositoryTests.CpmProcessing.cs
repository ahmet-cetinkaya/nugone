using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
using NuGone.FileSystem.Repositories;
using Shouldly;
using Xunit;

namespace NuGone.FileSystem.Tests.Repositories;

public sealed class SolutionRepositoryTests_CpmProcessing : SolutionRepositoryTests
{
    [Fact]
    public async Task Should_Detect_Cpm_In_Solution_Directory()
    {
        // Arrange
        var solutionPath = "/src/MySolution.sln";
        var propsPath = "/src/Directory.Packages.props";

        FileSystem.AddDirectory("/src");
        CreateSlnFile(solutionPath, "");
        CreateDirectoryPackagesProps(propsPath, enableCentralManagement: true);

        // Act
        var solution = await Repository.LoadSolutionAsync(solutionPath);

        // Assert
        solution.CentralPackageManagementEnabled.ShouldBeTrue();
        solution.DirectoryPackagesPropsPath.ShouldBe(propsPath);
    }

    [Fact]
    public async Task Should_Detect_Cpm_In_Parent_Directory()
    {
        // Arrange
        var solutionPath = "/src/projects/app/MySolution.sln";
        var propsPath = "/src/Directory.Packages.props";

        FileSystem.AddDirectory("/src/projects/app");
        CreateSlnFile(solutionPath, "");
        CreateDirectoryPackagesProps(propsPath, enableCentralManagement: true);

        // Act
        var solution = await Repository.LoadSolutionAsync(solutionPath);

        // Assert
        solution.CentralPackageManagementEnabled.ShouldBeTrue();
        solution.DirectoryPackagesPropsPath.ShouldBe(propsPath);
    }

    [Fact]
    public async Task Should_Detect_Cpm_In_Project_Directory_When_Missing_In_Solution_Directory()
    {
        // Arrange
        var projectPath = "/solution/src/project";
        FileSystem.Directory.CreateDirectory(projectPath);

        // Solution level - NO Directory.Packages.props

        // Project level - HAS Directory.Packages.props
        var projectCpmPath = Path.Combine(projectPath, "Directory.Packages.props");
        CreateDirectoryPackagesProps(projectCpmPath, enableCentralManagement: true);

        // Act
        var result = await Repository.CheckCentralPackageManagementAsync(projectPath);

        // Assert
        result.IsEnabled.ShouldBeTrue();
        result.DirectoryPackagesPropsPath.ShouldBe(projectCpmPath);
    }

    [Fact]
    public async Task Should_Resolve_Recursive_CPM_Imports()
    {
        // Arrange
        var rootPath = "/solution";
        var srcPath = "/solution/src";
        var projectPath = "/solution/src/project";
        FileSystem.Directory.CreateDirectory(projectPath); // Recursively creates src and root if needed in mock

        // 1. Root: ManageCentrally=true, NewtonSoft
        var rootPropsContent =
            @"
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include=""Newtonsoft.Json"" Version=""13.0.1"" />
  </ItemGroup>
</Project>";
        FileSystem.AddFile(
            Path.Combine(rootPath, "Directory.Packages.props"),
            new MockFileData(rootPropsContent)
        );

        // 2. Src: Import Root, NLog
        var srcPropsContent =
            @"
<Project>
  <Import Project=""../Directory.Packages.props"" />
  <ItemGroup>
    <PackageVersion Include=""NLog"" Version=""5.0.0"" />
  </ItemGroup>
</Project>";
        FileSystem.AddFile(
            Path.Combine(srcPath, "Directory.Packages.props"),
            new MockFileData(srcPropsContent)
        );

        // 3. Project: Import Src, Polly
        var projectPropsContent =
            @"
<Project>
  <Import Project=""../Directory.Packages.props"" />
  <ItemGroup>
    <PackageVersion Include=""Polly"" Version=""8.0.0"" />
  </ItemGroup>
</Project>";
        var projectCpmFile = Path.Combine(projectPath, "Directory.Packages.props");
        FileSystem.AddFile(projectCpmFile, new MockFileData(projectPropsContent));

        // Act - Check (should see enabled from root via chain)
        var checkResult = await Repository.CheckCentralPackageManagementAsync(projectPath);

        // Act - Load (should see all 3 packages)
        var versions = await Repository.LoadCentralPackageVersionsAsync(projectCpmFile);

        // Assert
        checkResult.IsEnabled.ShouldBeTrue();
        checkResult.DirectoryPackagesPropsPath.ShouldBe(projectCpmFile);

        versions.ShouldContainKey("Newtonsoft.Json");
        versions["Newtonsoft.Json"].ShouldBe("13.0.1");
        versions.ShouldContainKey("NLog");
        versions["NLog"].ShouldBe("5.0.0");
        versions.ShouldContainKey("Polly");
        versions["Polly"].ShouldBe("8.0.0");
    }

    [Fact]
    public async Task Should_Not_Detect_Cpm_If_Missing()
    {
        // Arrange
        var solutionPath = "/src/MySolution.sln";

        FileSystem.AddDirectory("/src");
        CreateSlnFile(solutionPath, "");

        // Act
        var solution = await Repository.LoadSolutionAsync(solutionPath);

        // Assert
        solution.CentralPackageManagementEnabled.ShouldBeFalse();
        solution.DirectoryPackagesPropsPath.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Not_Detect_Cpm_If_Disabled_In_Props()
    {
        // Arrange
        var solutionPath = "/src/MySolution.sln";
        var propsPath = "/src/Directory.Packages.props";

        FileSystem.AddDirectory("/src");
        CreateSlnFile(solutionPath, "");
        CreateDirectoryPackagesProps(propsPath, enableCentralManagement: false);

        // Act
        var solution = await Repository.LoadSolutionAsync(solutionPath);

        // Assert
        solution.CentralPackageManagementEnabled.ShouldBeFalse();
        // Even if disabled, we might want to know if the file exists, but the current logic
        // seems to treat disabled as "not enabled" and returns null path in the tuple check.
        // Let's verify current behavior or desired behavior.
        // The interface says "IsEnabled" and "Path". If not enabled, path might be irrelevant for the boolean flag.
        // But let's check what the entity expects.
        solution.DirectoryPackagesPropsPath.ShouldBeNull();
    }
}
