using System.IO.Abstractions.TestingHelpers;
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
