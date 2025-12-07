using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NuGone.NuGet.Repositories;
using Xunit;

namespace NuGone.NuGet.Tests.Repositories;

public class NuGetRepositoryCpmTests : IDisposable
{
    private readonly Mock<ILogger<NuGetRepository>> _mockLogger;
    private readonly NuGetRepository _repository;
    private readonly string _tempDirectory;

    public NuGetRepositoryCpmTests()
    {
        _mockLogger = new Mock<ILogger<NuGetRepository>>();
        _repository = new NuGetRepository(_mockLogger.Object);
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ExtractPackageReferencesAsync_Should_Resolve_Version_From_Central_Packages_When_Missing_In_Project()
    {
        // Arrange
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" />
              </ItemGroup>
            </Project>
            """;

        var projectFilePath = Path.Combine(_tempDirectory, "TestProject.csproj");
        await File.WriteAllTextAsync(projectFilePath, projectContent);

        var centralPackageVersions = new Dictionary<string, string>
        {
            { "Newtonsoft.Json", "13.0.3" },
        };

        // Act
        var packageReferences = await _repository.ExtractPackageReferencesAsync(
            projectFilePath,
            centralPackageVersions
        );

        // Assert
        packageReferences.Should().HaveCount(1);
        var package = packageReferences.First();
        package.PackageId.Should().Be("Newtonsoft.Json");
        package.Version.Should().Be("13.0.3");
    }

    [Fact]
    public async Task ExtractPackageReferencesAsync_Should_Prefer_Project_Version_Over_Central_Version()
    {
        // Arrange
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" Version="12.0.1" />
              </ItemGroup>
            </Project>
            """;

        var projectFilePath = Path.Combine(_tempDirectory, "TestProject.csproj");
        await File.WriteAllTextAsync(projectFilePath, projectContent);

        var centralPackageVersions = new Dictionary<string, string>
        {
            { "Newtonsoft.Json", "13.0.3" },
        };

        // Act
        var packageReferences = await _repository.ExtractPackageReferencesAsync(
            projectFilePath,
            centralPackageVersions
        );

        // Assert
        packageReferences.Should().HaveCount(1);
        var package = packageReferences.First();
        package.PackageId.Should().Be("Newtonsoft.Json");
        package.Version.Should().Be("12.0.1");
    }

    [Fact]
    public async Task ExtractPackageReferencesAsync_Should_Log_Warning_When_Version_Missing_And_Not_In_Central_Packages()
    {
        // Arrange
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" />
              </ItemGroup>
            </Project>
            """;

        var projectFilePath = Path.Combine(_tempDirectory, "TestProject.csproj");
        await File.WriteAllTextAsync(projectFilePath, projectContent);

        var centralPackageVersions = new Dictionary<string, string>
        {
            { "Other.Package", "1.0.0" },
        };

        // Act
        var packageReferences = await _repository.ExtractPackageReferencesAsync(
            projectFilePath,
            centralPackageVersions
        );

        // Assert
        packageReferences.Should().BeEmpty(); // Should skip the package without version

        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) =>
                            v.ToString()!.Contains("No version found for package: Newtonsoft.Json")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }
}
