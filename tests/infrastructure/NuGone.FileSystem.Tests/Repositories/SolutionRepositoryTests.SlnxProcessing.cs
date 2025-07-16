using NuGone.Domain.Features.PackageAnalysis.Entities;
using Shouldly;
using Xunit;

namespace NuGone.FileSystem.Tests.Repositories;

/// <summary>
/// Tests for .slnx solution file processing functionality.
/// Covers parsing, project discovery, and validation for the new .NET 9 .slnx format.
/// </summary>
public partial class SolutionRepositoryTests
{
    public class SlnxProcessing : SolutionRepositoryTests
    {
        [Fact]
        public async Task LoadSolutionAsync_WithValidSlnxFile_ShouldLoadSolutionWithProjects()
        {
            // Arrange
            var solutionPath = @"C:\Test\MySolution.slnx";
            var slnxContent =
                @"<Solution>
  <Project>
    <Path>src\Project1\Project1.csproj</Path>
  </Project>
  <Project>
    <Path>src\Project2\Project2.csproj</Path>
  </Project>
</Solution>";

            CreateSlnxFile(solutionPath, slnxContent);
            CreateProjectFile(@"C:\Test\src\Project1\Project1.csproj");
            CreateProjectFile(@"C:\Test\src\Project2\Project2.csproj");

            // Act
            var solution = await Repository.LoadSolutionAsync(solutionPath);

            // Assert
            solution.ShouldNotBeNull();
            solution.Name.ShouldBe("MySolution");
            solution.FilePath.ShouldBe(solutionPath);
            solution.Projects.ShouldHaveCount(2);
            solution.Projects.ShouldContain(p => p.Name == "Project1");
            solution.Projects.ShouldContain(p => p.Name == "Project2");
        }

        [Fact]
        public async Task LoadSolutionAsync_WithSlnxFileContainingMissingProjects_ShouldLoadOnlyExistingProjects()
        {
            // Arrange
            var solutionPath = @"C:\Test\MySolution.slnx";
            var slnxContent =
                @"<Solution>
  <Project>
    <Path>src\Project1\Project1.csproj</Path>
  </Project>
  <Project>
    <Path>src\MissingProject\MissingProject.csproj</Path>
  </Project>
</Solution>";

            CreateSlnxFile(solutionPath, slnxContent);
            CreateProjectFile(@"C:\Test\src\Project1\Project1.csproj");
            // MissingProject.csproj is intentionally not created

            // Act
            var solution = await Repository.LoadSolutionAsync(solutionPath);

            // Assert
            solution.ShouldNotBeNull();
            solution.Projects.ShouldHaveCount(1);
            solution.Projects.ShouldContain(p => p.Name == "Project1");
        }

        [Fact]
        public async Task LoadSolutionAsync_WithSlnxFileContainingEmptyProjectPath_ShouldSkipEmptyProjects()
        {
            // Arrange
            var solutionPath = @"C:\Test\MySolution.slnx";
            var slnxContent =
                @"<Solution>
  <Project>
    <Path>src\Project1\Project1.csproj</Path>
  </Project>
  <Project>
    <Path></Path>
  </Project>
</Solution>";

            CreateSlnxFile(solutionPath, slnxContent);
            CreateProjectFile(@"C:\Test\src\Project1\Project1.csproj");

            // Act
            var solution = await Repository.LoadSolutionAsync(solutionPath);

            // Assert
            solution.ShouldNotBeNull();
            solution.Projects.ShouldHaveCount(1);
            solution.Projects.ShouldContain(p => p.Name == "Project1");
        }

        [Fact]
        public async Task LoadSolutionAsync_WithSlnxFileContainingMissingPathElement_ShouldSkipInvalidProjects()
        {
            // Arrange
            var solutionPath = @"C:\Test\MySolution.slnx";
            var slnxContent =
                @"<Solution>
  <Project>
    <Path>src\Project1\Project1.csproj</Path>
  </Project>
  <Project>
    <Name>InvalidProject</Name>
  </Project>
</Solution>";

            CreateSlnxFile(solutionPath, slnxContent);
            CreateProjectFile(@"C:\Test\src\Project1\Project1.csproj");

            // Act
            var solution = await Repository.LoadSolutionAsync(solutionPath);

            // Assert
            solution.ShouldNotBeNull();
            solution.Projects.ShouldHaveCount(1);
            solution.Projects.ShouldContain(p => p.Name == "Project1");
        }

        [Fact]
        public async Task LoadSolutionAsync_WithSlnxFileContainingWhitespacePath_ShouldSkipWhitespaceProjects()
        {
            // Arrange
            var solutionPath = @"C:\Test\MySolution.slnx";
            var slnxContent =
                @"<Solution>
  <Project>
    <Path>src\Project1\Project1.csproj</Path>
  </Project>
  <Project>
    <Path>   </Path>
  </Project>
</Solution>";

            CreateSlnxFile(solutionPath, slnxContent);
            CreateProjectFile(@"C:\Test\src\Project1\Project1.csproj");

            // Act
            var solution = await Repository.LoadSolutionAsync(solutionPath);

            // Assert
            solution.ShouldNotBeNull();
            solution.Projects.ShouldHaveCount(1);
            solution.Projects.ShouldContain(p => p.Name == "Project1");
        }

        [Fact]
        public async Task LoadSolutionAsync_WithSlnxFileContainingRelativePaths_ShouldResolveCorrectly()
        {
            // Arrange
            var solutionPath = @"C:\Projects\MySolution\MySolution.slnx";
            var slnxContent =
                @"<Solution>
  <Project>
    <Path>..\Project1\Project1.csproj</Path>
  </Project>
  <Project>
    <Path>.\SubFolder\Project2\Project2.csproj</Path>
  </Project>
</Solution>";

            CreateSlnxFile(solutionPath, slnxContent);
            CreateProjectFile(@"C:\Projects\Project1\Project1.csproj");
            CreateProjectFile(@"C:\Projects\MySolution\SubFolder\Project2\Project2.csproj");

            // Act
            var solution = await Repository.LoadSolutionAsync(solutionPath);

            // Assert
            solution.ShouldNotBeNull();
            solution.Projects.ShouldHaveCount(2);
            solution.Projects.ShouldContain(p =>
                p.FilePath == @"C:\Projects\Project1\Project1.csproj"
            );
            solution.Projects.ShouldContain(p =>
                p.FilePath == @"C:\Projects\MySolution\SubFolder\Project2\Project2.csproj"
            );
        }

        [Fact]
        public async Task LoadSolutionAsync_WithSlnxFileContainingXmlNamespace_ShouldParseCorrectly()
        {
            // Arrange
            var solutionPath = @"C:\Test\MySolution.slnx";
            var slnxContent =
                @"<Solution xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Project>
    <Path>src\Project1\Project1.csproj</Path>
  </Project>
</Solution>";

            CreateSlnxFile(solutionPath, slnxContent);
            CreateProjectFile(@"C:\Test\src\Project1\Project1.csproj");

            // Act
            var solution = await Repository.LoadSolutionAsync(solutionPath);

            // Assert
            solution.ShouldNotBeNull();
            solution.Projects.ShouldHaveCount(1);
            solution.Projects.ShouldContain(p => p.Name == "Project1");
        }

        [Fact]
        public async Task LoadSolutionAsync_WithSlnxFileContainingMultipleProjectsWithSameName_ShouldLoadAllProjects()
        {
            // Arrange
            var solutionPath = @"C:\Test\MySolution.slnx";
            var slnxContent =
                @"<Solution>
  <Project>
    <Path>src\Project1\Project1.csproj</Path>
  </Project>
  <Project>
    <Path>tests\Project1.Tests\Project1.Tests.csproj</Path>
  </Project>
</Solution>";

            CreateSlnxFile(solutionPath, slnxContent);
            CreateProjectFile(@"C:\Test\src\Project1\Project1.csproj");
            CreateProjectFile(@"C:\Test\tests\Project1.Tests\Project1.Tests.csproj");

            // Act
            var solution = await Repository.LoadSolutionAsync(solutionPath);

            // Assert
            solution.ShouldNotBeNull();
            solution.Projects.ShouldHaveCount(2);
            solution.Projects.ShouldContain(p =>
                p.FilePath == @"C:\Test\src\Project1\Project1.csproj"
            );
            solution.Projects.ShouldContain(p =>
                p.FilePath == @"C:\Test\tests\Project1.Tests\Project1.Tests.csproj"
            );
        }

        [Fact]
        public async Task LoadSolutionAsync_WithSlnxFileContainingNestedProjectStructure_ShouldLoadAllProjects()
        {
            // Arrange
            var solutionPath = @"C:\Test\MySolution.slnx";
            var slnxContent =
                @"<Solution>
  <Project>
    <Path>src\Core\Project1\Project1.csproj</Path>
  </Project>
  <Project>
    <Path>src\Core\Project2\Project2.csproj</Path>
  </Project>
  <Project>
    <Path>src\Services\Service1\Service1.csproj</Path>
  </Project>
</Solution>";

            CreateSlnxFile(solutionPath, slnxContent);
            CreateProjectFile(@"C:\Test\src\Core\Project1\Project1.csproj");
            CreateProjectFile(@"C:\Test\src\Core\Project2\Project2.csproj");
            CreateProjectFile(@"C:\Test\src\Services\Service1\Service1.csproj");

            // Act
            var solution = await Repository.LoadSolutionAsync(solutionPath);

            // Assert
            solution.ShouldNotBeNull();
            solution.Projects.ShouldHaveCount(3);
            solution.Projects.ShouldContain(p => p.Name == "Project1");
            solution.Projects.ShouldContain(p => p.Name == "Project2");
            solution.Projects.ShouldContain(p => p.Name == "Service1");
        }

        [Fact]
        public async Task LoadSolutionAsync_WithEmptySlnxFile_ShouldLoadSolutionWithNoProjects()
        {
            // Arrange
            var solutionPath = @"C:\Test\EmptySolution.slnx";
            var slnxContent = @"<Solution></Solution>";

            CreateSlnxFile(solutionPath, slnxContent);

            // Act
            var solution = await Repository.LoadSolutionAsync(solutionPath);

            // Assert
            solution.ShouldNotBeNull();
            solution.Name.ShouldBe("EmptySolution");
            solution.Projects.ShouldBeEmpty();
        }

        [Fact]
        public async Task LoadSolutionAsync_WithSlnxFileContainingOnlyProjectContainer_ShouldLoadSolutionWithNoProjects()
        {
            // Arrange
            var solutionPath = @"C:\Test\MySolution.slnx";
            var slnxContent =
                @"<Solution>
  <Project></Project>
</Solution>";

            CreateSlnxFile(solutionPath, slnxContent);

            // Act
            var solution = await Repository.LoadSolutionAsync(solutionPath);

            // Assert
            solution.ShouldNotBeNull();
            solution.Projects.ShouldBeEmpty();
        }
    }
}
