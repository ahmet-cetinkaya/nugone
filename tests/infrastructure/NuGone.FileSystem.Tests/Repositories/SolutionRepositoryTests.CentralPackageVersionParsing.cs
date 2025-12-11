using System.IO.Abstractions.TestingHelpers;
using NuGone.FileSystem.Repositories;
using Shouldly;
using Xunit;

namespace NuGone.FileSystem.Tests.Repositories;

public sealed class SolutionRepositoryTests_CentralPackageVersionParsing : SolutionRepositoryTests
{
    [Fact]
    public async Task Should_Parse_Multiple_Packages_Correctly()
    {
        // Arrange
        var solutionPath = "/src/MySolution.sln";
        var propsPath = "/src/Directory.Packages.props";

        FileSystem.AddDirectory("/src");
        CreateSlnFile(solutionPath, string.Empty);

        var propsContent = """
<Project>
  <ItemGroup>
    <PackageVersion Include="Package.A" Version="1.2.3" />
    <PackageVersion Include="Package.B" Version="4.5.6" />
    <PackageVersion Include="Package.C" Version="7.8.9" />
  </ItemGroup>
</Project>
""";

        FileSystem.AddFile(propsPath, new MockFileData(propsContent));

        // Act
        var result = await Repository.LoadCentralPackageVersionsAsync(
            propsPath,
            CancellationToken.None
        );

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
        result["Package.A"].ShouldBe("1.2.3");
        result["Package.B"].ShouldBe("4.5.6");
        result["Package.C"].ShouldBe("7.8.9");
    }

    [Fact]
    public async Task Should_Handle_Empty_Version_Attributes()
    {
        // Arrange
        var solutionPath = "/src/MySolution.sln";
        var propsPath = "/src/Directory.Packages.props";

        FileSystem.AddDirectory("/src");
        CreateSlnFile(solutionPath, string.Empty);

        var propsContent = """
<Project>
  <ItemGroup>
    <PackageVersion Include="Package.WithVersion" Version="1.0.0" />
    <PackageVersion Include="Package.WithoutVersion" />
    <PackageVersion Include="Package.WithEmptyVersion" Version="  " />
    <PackageVersion Include="Package.WithNullVersion" Version="" />
  </ItemGroup>
</Project>
""";

        FileSystem.AddFile(propsPath, new MockFileData(propsContent));

        // Act
        var result = await Repository.LoadCentralPackageVersionsAsync(
            propsPath,
            CancellationToken.None
        );

        // Assert
        result.ShouldNotBeNull();
        result.ContainsKey("Package.WithVersion").ShouldBeTrue();
        result["Package.WithVersion"].ShouldBe("1.0.0");

        result.ContainsKey("Package.WithoutVersion").ShouldBeFalse();
        result.ContainsKey("Package.WithEmptyVersion").ShouldBeFalse();
        result.ContainsKey("Package.WithNullVersion").ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Handle_Missing_Include_Attributes()
    {
        // Arrange
        var solutionPath = "/src/MySolution.sln";
        var propsPath = "/src/Directory.Packages.props";

        FileSystem.AddDirectory("/src");
        CreateSlnFile(solutionPath, string.Empty);

        var propsContent = """
<Project>
  <ItemGroup>
    <PackageVersion Include="Valid.Package" Version="1.0.0" />
    <PackageVersion Version="2.0.0" />
    <PackageVersion />
  </ItemGroup>
</Project>
""";

        FileSystem.AddFile(propsPath, new MockFileData(propsContent));

        // Act
        var result = await Repository.LoadCentralPackageVersionsAsync(
            propsPath,
            CancellationToken.None
        );

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result.ContainsKey("Valid.Package").ShouldBeTrue();
        result["Valid.Package"].ShouldBe("1.0.0");
    }

    [Fact]
    public async Task Should_Throw_On_Malformed_Xml()
    {
        // Arrange
        var solutionPath = "/src/MySolution.sln";
        var propsPath = "/src/Directory.Packages.props";

        FileSystem.AddDirectory("/src");
        CreateSlnFile(solutionPath, string.Empty);

        var malformedContent = "this is not valid xml";

        FileSystem.AddFile(propsPath, new MockFileData(malformedContent));

        // Act & Assert
        await Should.ThrowAsync<System.Xml.XmlException>(() =>
            Repository.LoadCentralPackageVersionsAsync(propsPath, CancellationToken.None)
        );
    }

    [Fact]
    public async Task Should_Handle_Case_Insensitive_Package_Lookup()
    {
        // Arrange
        var solutionPath = "/src/MySolution.sln";
        var propsPath = "/src/Directory.Packages.props";

        FileSystem.AddDirectory("/src");
        CreateSlnFile(solutionPath, string.Empty);

        var propsContent = """
<Project>
  <ItemGroup>
    <PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageVersion Include="Microsoft.Extensions.Logging" Version="9.0.0" />
  </ItemGroup>
</Project>
""";

        FileSystem.AddFile(propsPath, new MockFileData(propsContent));

        // Act
        var result = await Repository.LoadCentralPackageVersionsAsync(
            propsPath,
            CancellationToken.None
        );

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);

        // Test case-insensitive lookup
        result.ContainsKey("newtonsoft.json").ShouldBeTrue();
        result.ContainsKey("NEWTONSOFT.JSON").ShouldBeTrue();
        result.ContainsKey("microsoft.extensions.logging").ShouldBeTrue();
        result.ContainsKey("MICROSOFT.EXTENSIONS.LOGGING").ShouldBeTrue();

        // Verify values are accessible with different cases
        result["newtonsoft.json"].ShouldBe("13.0.3");
        result["NEWTONSOFT.JSON"].ShouldBe("13.0.3");
        result["microsoft.extensions.logging"].ShouldBe("9.0.0");
        result["MICROSOFT.EXTENSIONS.LOGGING"].ShouldBe("9.0.0");
    }

    [Fact]
    public async Task Should_Handle_Duplicate_Package_Entries()
    {
        // Arrange
        var solutionPath = "/src/MySolution.sln";
        var propsPath = "/src/Directory.Packages.props";

        FileSystem.AddDirectory("/src");
        CreateSlnFile(solutionPath, string.Empty);

        var propsContent = """
<Project>
  <ItemGroup>
    <PackageVersion Include="Duplicate.Package" Version="1.0.0" />
    <PackageVersion Include="Another.Package" Version="2.0.0" />
    <PackageVersion Include="Duplicate.Package" Version="3.0.0" />
  </ItemGroup>
</Project>
""";

        FileSystem.AddFile(propsPath, new MockFileData(propsContent));

        // Act
        var result = await Repository.LoadCentralPackageVersionsAsync(
            propsPath,
            CancellationToken.None
        );

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);

        // The last occurrence should win
        result["Duplicate.Package"].ShouldBe("3.0.0");
        result["Another.Package"].ShouldBe("2.0.0");
    }

    [Fact]
    public async Task Should_Return_Empty_Dictionary_When_No_PackageVersion_Elements()
    {
        // Arrange
        var solutionPath = "/src/MySolution.sln";
        var propsPath = "/src/Directory.Packages.props";

        FileSystem.AddDirectory("/src");
        CreateSlnFile(solutionPath, string.Empty);

        var propsContent = """
<Project>
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="Some.Reference" />
  </ItemGroup>
</Project>
""";

        FileSystem.AddFile(propsPath, new MockFileData(propsContent));

        // Act
        var result = await Repository.LoadCentralPackageVersionsAsync(
            propsPath,
            CancellationToken.None
        );

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Handle_Comments_And_Whitespace()
    {
        // Arrange
        var solutionPath = "/src/MySolution.sln";
        var propsPath = "/src/Directory.Packages.props";

        FileSystem.AddDirectory("/src");
        CreateSlnFile(solutionPath, string.Empty);

        var propsContent = """
<Project>
  <!-- This is a comment -->
  <ItemGroup>

    <!-- Package with comment before -->
    <PackageVersion Include="Package.One" Version="1.0.0" />

    <!-- Package with comment after -->
    <PackageVersion Include="Package.Two" Version="2.0.0" /> <!-- Inline comment -->

  </ItemGroup>
  <!-- Another comment -->
</Project>
""";

        FileSystem.AddFile(propsPath, new MockFileData(propsContent));

        // Act
        var result = await Repository.LoadCentralPackageVersionsAsync(
            propsPath,
            CancellationToken.None
        );

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result["Package.One"].ShouldBe("1.0.0");
        result["Package.Two"].ShouldBe("2.0.0");
    }

    [Fact]
    public async Task Should_Handle_Special_Characters_In_Package_Names()
    {
        // Arrange
        var solutionPath = "/src/MySolution.sln";
        var propsPath = "/src/Directory.Packages.props";

        FileSystem.AddDirectory("/src");
        CreateSlnFile(solutionPath, string.Empty);

        var propsContent = """
<Project>
  <ItemGroup>
    <PackageVersion Include="System.Text.Json" Version="9.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Configuration.Abstractions" Version="9.0.0" />
    <PackageVersion Include="NLog.Extensions.Logging" Version="5.3.0" />
  </ItemGroup>
</Project>
""";

        FileSystem.AddFile(propsPath, new MockFileData(propsContent));

        // Act
        var result = await Repository.LoadCentralPackageVersionsAsync(
            propsPath,
            CancellationToken.None
        );

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
        result["System.Text.Json"].ShouldBe("9.0.0");
        result["Microsoft.Extensions.Configuration.Abstractions"].ShouldBe("9.0.0");
        result["NLog.Extensions.Logging"].ShouldBe("5.3.0");
    }
}
