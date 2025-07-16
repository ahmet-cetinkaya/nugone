using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using NuGone.FileSystem.Repositories;
using Xunit;

namespace NuGone.FileSystem.Tests.Repositories;

/// <summary>
/// Base test class for SolutionRepository tests.
/// Provides common setup and helper methods for testing solution file processing.
/// </summary>
public abstract partial class SolutionRepositoryTests
{
    protected readonly MockFileSystem FileSystem;
    protected readonly Mock<ILogger<SolutionRepository>> LoggerMock;
    protected readonly SolutionRepository Repository;

    protected SolutionRepositoryTests()
    {
        FileSystem = new MockFileSystem();
        LoggerMock = new Mock<ILogger<SolutionRepository>>();
        Repository = new SolutionRepository(FileSystem, LoggerMock.Object);
    }

    /// <summary>
    /// Creates a test .slnx file with the specified content.
    /// </summary>
    protected void CreateSlnxFile(string path, string content)
    {
        FileSystem.AddFile(path, new MockFileData(content));
    }

    /// <summary>
    /// Creates a test .sln file with the specified content.
    /// </summary>
    protected void CreateSlnFile(string path, string content)
    {
        FileSystem.AddFile(path, new MockFileData(content));
    }

    /// <summary>
    /// Creates a test project file at the specified path.
    /// </summary>
    protected void CreateProjectFile(string path, string content = "")
    {
        FileSystem.AddFile(path, new MockFileData(content));
    }

    /// <summary>
    /// Creates a test Directory.Packages.props file.
    /// </summary>
    protected void CreateDirectoryPackagesProps(string path, bool enableCentralManagement = true)
    {
        var content =
            $@"<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>{enableCentralManagement.ToString().ToLower()}</ManagePackageVersionsCentrally>
  </PropertyGroup>
</Project>";
        FileSystem.AddFile(path, new MockFileData(content));
    }
}
