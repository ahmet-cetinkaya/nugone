using Shouldly;
using Microsoft.Extensions.Logging;
using Moq;
using NuGone.NuGet.Repositories;
using Xunit;

#pragma warning disable CA1873 // Avoid potentially expensive logging in test verifications

namespace NuGone.NuGet.Tests.Repositories;

public sealed class NuGetRepositoryCpmTests : IDisposable
{
    private readonly Mock<ILogger<NuGetRepository>> _mockLogger;
    private readonly NuGetRepository _repository;
    private readonly string _tempDirectory;

    public NuGetRepositoryCpmTests()
    {
        _mockLogger = new Mock<ILogger<NuGetRepository>>();
        // Enable log levels so LoggerMessage source generator actually logs
        _mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        _repository = new NuGetRepository(_mockLogger.Object);
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }
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
        packageReferences.Count().ShouldBe(1);
        var package = packageReferences.First();
        package.PackageId.ShouldBe("Newtonsoft.Json");
        package.Version.ShouldBe("13.0.3");
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
        packageReferences.Count().ShouldBe(1);
        var package = packageReferences.First();
        package.PackageId.ShouldBe("Newtonsoft.Json");
        package.Version.ShouldBe("12.0.1");
    }

    [Fact]
    public async Task ExtractPackageReferencesAsync_Should_Skip_Package_When_Version_Missing_And_Not_In_Central_Packages()
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

        // Assert - Package should be skipped when version is missing and not in central packages
        packageReferences.ShouldBeEmpty();

        // Assert - Warning should be logged for missing package version
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
