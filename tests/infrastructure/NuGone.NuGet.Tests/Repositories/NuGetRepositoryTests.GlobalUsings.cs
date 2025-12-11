using Microsoft.Extensions.Logging;
using Moq;
using NuGone.Domain.Features.PackageAnalysis.Entities;
using NuGone.NuGet.Repositories;
using Shouldly;
using Xunit;

namespace NuGone.NuGet.Tests.Repositories;

/// <summary>
/// Tests for global using functionality in NuGetRepository.
/// </summary>
public sealed class NuGetRepositoryGlobalUsingsTests : IDisposable
{
    private readonly Mock<ILogger<NuGetRepository>> _mockLogger;
    private readonly NuGetRepository _repository;
    private readonly string _tempDirectory;

    public NuGetRepositoryGlobalUsingsTests()
    {
        _mockLogger = new Mock<ILogger<NuGetRepository>>();
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
    public async Task ExtractGlobalUsingsAsync_WithValidProjectFile_ShouldExtractGlobalUsings()
    {
        // Arrange
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net9.0</TargetFramework>
              </PropertyGroup>
              
              <ItemGroup>
                <PackageReference Include="Xunit" Version="2.4.2" />
                <PackageReference Include="Moq" Version="4.18.4" />
              </ItemGroup>
              
              <ItemGroup>
                <Using Include="Xunit" />
                <Using Include="Moq" />
              </ItemGroup>
            </Project>
            """;

        var projectFilePath = Path.Combine(_tempDirectory, "TestProject.csproj");
        await File.WriteAllTextAsync(projectFilePath, projectContent);

        // Act
        var globalUsings = await _repository.ExtractGlobalUsingsAsync(projectFilePath);

        // Assert
        globalUsings.Count().ShouldBe(2);
        globalUsings.ShouldContain(gu => gu.PackageId == "Xunit");
        globalUsings.ShouldContain(gu => gu.PackageId == "Moq");
        foreach (var gu in globalUsings)
        {
            gu.ProjectPath.ShouldBe(projectFilePath);
        }
    }

    [Fact]
    public async Task ExtractGlobalUsingsAsync_WithConditionalGlobalUsings_ShouldExtractWithConditions()
    {
        // Arrange
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net9.0</TargetFramework>
              </PropertyGroup>
              
              <ItemGroup>
                <PackageReference Include="Xunit" Version="2.4.2" />
                <PackageReference Include="Moq" Version="4.18.4" Condition="'$(Configuration)' == 'Debug'" />
              </ItemGroup>
              
              <ItemGroup>
                <Using Include="Xunit" />
                <Using Include="Moq" Condition="'$(Configuration)' == 'Debug'" />
              </ItemGroup>
            </Project>
            """;

        var projectFilePath = Path.Combine(_tempDirectory, "TestProject.csproj");
        await File.WriteAllTextAsync(projectFilePath, projectContent);

        // Act
        var globalUsings = await _repository.ExtractGlobalUsingsAsync(projectFilePath);

        // Assert
        globalUsings.Count().ShouldBe(2);

        var xunitUsing = globalUsings.First(gu => gu.PackageId == "Xunit");
        xunitUsing.Condition.ShouldBe(null);

        var moqUsing = globalUsings.First(gu => gu.PackageId == "Moq");
        moqUsing.Condition.ShouldBe("'$(Configuration)' == 'Debug'");
    }

    [Fact]
    public async Task ExtractGlobalUsingsAsync_WithNoGlobalUsings_ShouldReturnEmpty()
    {
        // Arrange
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net9.0</TargetFramework>
              </PropertyGroup>
              
              <ItemGroup>
                <PackageReference Include="Xunit" Version="2.4.2" />
                <PackageReference Include="Moq" Version="4.18.4" />
              </ItemGroup>
            </Project>
            """;

        var projectFilePath = Path.Combine(_tempDirectory, "TestProject.csproj");
        await File.WriteAllTextAsync(projectFilePath, projectContent);

        // Act
        var globalUsings = await _repository.ExtractGlobalUsingsAsync(projectFilePath);

        // Assert
        globalUsings.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExtractPackageReferencesAsync_WithGlobalUsings_ShouldMarkPackagesWithGlobalUsings()
    {
        // Arrange
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net9.0</TargetFramework>
              </PropertyGroup>
              
              <ItemGroup>
                <PackageReference Include="Xunit" Version="2.4.2" />
                <PackageReference Include="Moq" Version="4.18.4" />
                <PackageReference Include="Shouldly" Version="4.2.1" />
              </ItemGroup>
              
              <ItemGroup>
                <Using Include="Xunit" />
                <Using Include="Moq" />
              </ItemGroup>
            </Project>
            """;

        var projectFilePath = Path.Combine(_tempDirectory, "TestProject.csproj");
        await File.WriteAllTextAsync(projectFilePath, projectContent);

        // Act
        var packageReferences = await _repository.ExtractPackageReferencesAsync(projectFilePath);

        // Assert
        packageReferences.Count().ShouldBe(3);

        var xunitPackage = packageReferences.First(pr => pr.PackageId == "Xunit");
        xunitPackage.HasGlobalUsing.ShouldBeTrue();

        var moqPackage = packageReferences.First(pr => pr.PackageId == "Moq");
        moqPackage.HasGlobalUsing.ShouldBeTrue();

        var shouldlyPackage = packageReferences.First(pr => pr.PackageId == "Shouldly");
        shouldlyPackage.HasGlobalUsing.ShouldBeFalse();
    }

    [Fact]
    public async Task ExtractGlobalUsingsAsync_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDirectory, "NonExistent.csproj");

        // Act & Assert
        var exception = await Should.ThrowAsync<FileNotFoundException>(() =>
            _repository.ExtractGlobalUsingsAsync(nonExistentPath)
        );
        exception.Message.ShouldBe($"Project file not found: {nonExistentPath}");
    }

    [Fact]
    public async Task ExtractGlobalUsingsAsync_WithInvalidXml_ShouldThrowException()
    {
        // Arrange
        var invalidXmlContent = "<Project><InvalidXml>";
        var projectFilePath = Path.Combine(_tempDirectory, "InvalidProject.csproj");
        await File.WriteAllTextAsync(projectFilePath, invalidXmlContent);

        // Act & Assert
        await Should.ThrowAsync<Exception>(() =>
            _repository.ExtractGlobalUsingsAsync(projectFilePath)
        );
    }

    [Fact]
    public async Task ExtractGlobalUsingsAsync_WithEmptyIncludeAttribute_ShouldSkipEmptyEntries()
    {
        // Arrange
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net9.0</TargetFramework>
              </PropertyGroup>
              
              <ItemGroup>
                <Using Include="Xunit" />
                <Using Include="" />
                <Using />
                <Using Include="Moq" />
              </ItemGroup>
            </Project>
            """;

        var projectFilePath = Path.Combine(_tempDirectory, "TestProject.csproj");
        await File.WriteAllTextAsync(projectFilePath, projectContent);

        // Act
        var globalUsings = await _repository.ExtractGlobalUsingsAsync(projectFilePath);

        // Assert
        globalUsings.Count().ShouldBe(2);
        globalUsings.ShouldContain(gu => gu.PackageId == "Xunit");
        globalUsings.ShouldContain(gu => gu.PackageId == "Moq");
    }
}
