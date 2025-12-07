using System.Xml;
using Shouldly;
using Xunit;

namespace NuGone.Cli.Tests.Features.ToolConfiguration;

/// <summary>
/// Tests for DotnetToolSettings.xml configuration.
/// Tests the critical aspects of global tool functionality.
/// </summary>
public class DotnetToolSettingsTests
{
    private readonly string _solutionRoot;
    private readonly string _cliProjectPath;
    private readonly string _dotnetToolSettingsPath;
    private readonly string _projectFilePath;

    public DotnetToolSettingsTests()
    {
        // Get the solution root directory by searching for .sln file
        var currentDir = Directory.GetCurrentDirectory();
        _solutionRoot = FindSolutionRoot(currentDir);
        _cliProjectPath = Path.Combine(_solutionRoot, "src", "presentation", "NuGone.Cli");
        _dotnetToolSettingsPath = Path.Combine(_cliProjectPath, "DotnetToolSettings.xml");
        _projectFilePath = Path.Combine(_cliProjectPath, "NuGone.Cli.csproj");
    }

    private static string FindSolutionRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir != null)
        {
            // Look for both .sln and .slnx files
            var slnFiles = dir.GetFiles("*.sln");
            var slnxFiles = dir.GetFiles("*.slnx");
            if (slnFiles.Length > 0 || slnxFiles.Length > 0)
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not find solution root directory");
    }

    [Fact]
    public void DotnetToolSettingsXml_ShouldExist()
    {
        // Act
        var exists = File.Exists(_dotnetToolSettingsPath);

        // Assert
        exists.ShouldBeTrue($"DotnetToolSettings.xml should exist at {_dotnetToolSettingsPath}");
    }

    [Fact]
    public void DotnetToolSettingsXml_ShouldHaveCorrectEntryPoint()
    {
        // Arrange
        File.Exists(_dotnetToolSettingsPath).ShouldBeTrue();

        // Act
        var content = File.ReadAllText(_dotnetToolSettingsPath);
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(content);

        // Assert
        var commandNode = xmlDoc.SelectSingleNode("//Command");
        commandNode.ShouldNotBeNull("Command node should exist");

        var entryPointAttribute = commandNode?.Attributes?["EntryPoint"];
        entryPointAttribute?.Value.ShouldBe(
            "nugone.dll",
            "EntryPoint should be 'nugone.dll' to match assembly name"
        );
    }

    [Fact]
    public void DotnetToolSettingsXml_ShouldHaveCorrectCommandName()
    {
        // Arrange
        File.Exists(_dotnetToolSettingsPath).ShouldBeTrue();

        // Act
        var content = File.ReadAllText(_dotnetToolSettingsPath);
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(content);

        // Assert
        var commandNode = xmlDoc.SelectSingleNode("//Command");
        var nameAttribute = commandNode?.Attributes?["Name"];
        nameAttribute?.Value.ShouldBe("nugone", "Command name should be 'nugone'");
    }

    [Fact]
    public void ProjectFile_ShouldHaveCorrectConfiguration()
    {
        // Arrange & Act
        var content = File.ReadAllText(_projectFilePath);
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(content);

        // Assert
        content.ShouldContain("<PackAsTool>true</PackAsTool>");
        content.ShouldContain("<ToolCommandName>nugone</ToolCommandName>");
        content.ShouldContain("<AssemblyName>nugone</AssemblyName>");
    }

    [Fact]
    public void ProjectFile_ShouldNotManuallyPackageDotnetToolSettingsXml()
    {
        // Arrange & Act
        var content = File.ReadAllText(_projectFilePath);

        // Assert - Should NOT contain manual packaging of DotnetToolSettings.xml
        content.ShouldNotContain("DotnetToolSettings.xml\" Pack=\"true\"");
    }

    [Fact]
    public void AssemblyNameAndCommandName_ShouldMatch()
    {
        // Arrange
        var projectContent = File.ReadAllText(_projectFilePath);
        var xmlContent = File.ReadAllText(_dotnetToolSettingsPath);
        var projectXml = new XmlDocument();
        var toolXml = new XmlDocument();
        projectXml.LoadXml(projectContent);
        toolXml.LoadXml(xmlContent);

        // Act
        var assemblyNameNode = projectXml.SelectSingleNode("//PropertyGroup/AssemblyName");
        var commandNode = toolXml.SelectSingleNode("//Command");
        var entryPointAttribute = commandNode?.Attributes?["EntryPoint"];
        var nameAttribute = commandNode?.Attributes?["Name"];

        // Assert
        var assemblyName = assemblyNameNode?.InnerText;
        var entryPoint = entryPointAttribute?.Value;
        var commandName = nameAttribute?.Value;

        assemblyName.ShouldBe("nugone", "Assembly name should be nugone");
        entryPoint.ShouldBe("nugone.dll", "EntryPoint should be nugone.dll");
        commandName.ShouldBe("nugone", "Command name should be nugone");

        // All should be consistent
        assemblyName.ShouldBe("nugone");
        entryPoint.ShouldBe("nugone.dll");
        commandName.ShouldBe("nugone");
    }
}
