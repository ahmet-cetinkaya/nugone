# 📖 NuGone User Guide

This comprehensive guide covers everything you need to know about using NuGone to detect and remove unused NuGet packages in your .NET projects.

## 📋 Table of Contents

1. [Installation](#installation)
2. [Quick Start](#quick-start)
3. [Commands](#commands)
4. [Configuration](#configuration)
5. [Advanced Usage](#advanced-usage)
6. [Integration Examples](#integration-examples)
7. [Troubleshooting](#troubleshooting)
8. [Frequently Asked Questions](#frequently-asked-questions)

## 🚀 Installation

### Prerequisites

- .NET 9.0 or later
- Windows, macOS, or Linux

### Install as Global Tool

```bash
dotnet tool install --global nugone
```

### Verify Installation

```bash
nugone --version
```

### Update to Latest Version

```bash
dotnet tool update --global nugone
```

### Uninstall

```bash
dotnet tool uninstall --global nugone
```

## ⚡ Quick Start

### Analyze a Single Project

```bash
nugone analyze --project MyProject.csproj
```

### Analyze an Entire Solution

```bash
nugone analyze --project MySolution.sln
```

### Get JSON Output for CI/CD

```bash
nugone analyze --project MySolution.sln --format json --output results.json
```

## 🎯 Commands

### Analyze Command

The `analyze` command scans your projects for unused NuGet packages without making any changes.

#### Syntax

```bash
nugone analyze [options]
```

#### Options

| Option      | Short | Description                      | Example                     |
| ----------- | ----- | -------------------------------- | --------------------------- |
| `--project` | `-p`  | Path to project or solution file | `--project MySolution.sln`  |
| `--format`  | `-f`  | Output format (`text` or `json`) | `--format json`             |
| `--output`  | `-o`  | Write report to file             | `--output report.json`      |
| `--exclude` | `-e`  | Exclude specific packages        | `--exclude Newtonsoft.Json` |
| `--verbose` | `-v`  | Show detailed output             | `--verbose`                 |

#### Examples

**Basic Analysis**

```bash
nugone analyze --project MyProject.csproj
```

**JSON Output to File**

```bash
nugone analyze --project MySolution.sln --format json --output unused-packages.json
```

**Exclude Specific Packages**

```bash
nugone analyze --project MyProject.csproj --exclude Newtonsoft.Json --exclude AutoMapper
```

**Verbose Output**

```bash
nugone analyze --project MySolution.sln --verbose
```

### Remove Command (Planned)

The `remove` command will allow safe removal of unused packages. This feature is planned for a future release.

## ⚙️ Configuration

NuGone supports configuration through `global.json` or a dedicated configuration file.

### Using global.json (Recommended)

Create or update `global.json` in your solution root:

```json
{
    "sdk": {
        "version": "8.0.0"
    },
    "nugone": {
        "excludeNamespaces": [
            "System.Text.Json",
            "Microsoft.Extensions.Logging"
        ],
        "excludeFiles": [
            "**/*.Designer.cs",
            "**/Generated/**",
            "**/obj/**",
            "**/bin/**"
        ],
        "excludePackages": ["Microsoft.NET.Test.Sdk", "coverlet.collector"]
    }
}
```

### Configuration Options

| Option              | Type  | Description                         | Example                      |
| ------------------- | ----- | ----------------------------------- | ---------------------------- |
| `excludeNamespaces` | array | Namespaces to exclude from analysis | `["System.Text.Json"]`       |
| `excludeFiles`      | array | File patterns to exclude            | `["**/*.Designer.cs"]`       |
| `excludePackages`   | array | Packages to never mark as unused    | `["Microsoft.NET.Test.Sdk"]` |

### Legacy Configuration File

If you don't have `global.json`, NuGone will also look for a `nugone.config.json` file:

```json
{
    "excludeNamespaces": ["System.Text.Json"],
    "excludeFiles": ["**/*.Designer.cs"],
    "excludePackages": ["Microsoft.NET.Test.Sdk"]
}
```

## 🔧 Advanced Usage

### Multi-Target Frameworks

NuGone automatically handles projects targeting multiple frameworks:

```xml
<PropertyGroup>
  <TargetFrameworks>net8.0;net9.0</TargetFrameworks>
</PropertyGroup>
```

### Central Package Management

NuGone detects and works with central package management (Directory.Packages.props):

```xml
<Project>
  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" />
  </ItemGroup>
</Project>
```

### Conditional Package References

Packages with conditions are properly evaluated:

```xml
<ItemGroup Condition="'$(Configuration)' == 'Debug'">
  <PackageReference Include="Microsoft.Extensions.Logging" />
</ItemGroup>
```

### Global Using Directives

NuGone detects and respects global using statements:

```csharp
// GlobalUsings.cs
global using System.Text.Json;
global using Microsoft.Extensions.Logging;
```

## 🔗 Integration Examples

### GitHub Actions

```yaml
name: Analyze Unused Packages

on: [push, pull_request]

jobs:
    analyze:
        runs-on: ubuntu-latest
        steps:
            - uses: actions/checkout@v3

            - name: Setup .NET
              uses: actions/setup-dotnet@v3
              with:
                  dotnet-version: 8.0.x

            - name: Install NuGone
              run: dotnet tool install --global nugone

            - name: Analyze packages
              run: |
                  nugone analyze --project . --format json --output unused-packages.json

            - name: Upload results
              uses: actions/upload-artifact@v3
              with:
                  name: package-analysis
                  path: unused-packages.json
```

### Azure DevOps

```yaml
trigger:
    - main

pool:
    vmImage: "ubuntu-latest"

steps:
    - task: UseDotNet@2
      inputs:
          packageType: "sdk"
          version: "8.x"

    - script: dotnet tool install --global nugone
      displayName: "Install NuGone"

    - script: nugone analyze --project . --format json --output unused-packages.json
      displayName: "Analyze unused packages"

    - task: PublishBuildArtifacts@1
      inputs:
          pathtoPublish: "unused-packages.json"
          artifactName: "package-analysis"
```

### PowerShell Script

```powershell
# Analyze and fail script if unused packages found
$nugoneResults = nugone analyze --project . --format json | ConvertFrom-Json

if ($nugoneResults.unusedPackages.Count -gt 0) {
    Write-Host "Found $($nugoneResults.unusedPackages.Count) unused packages:"
    $nugoneResults.unusedPackages | ForEach-Object {
        Write-Host "- $($_.name) v$($_.version)"
    }
    exit 1
} else {
    Write-Host "No unused packages found. ✅"
}
```

### Bash Script

```bash
#!/bin/bash

# Analyze and fail if unused packages found
nugone analyze --project . --format json > unused-packages.json
unused_count=$(jq '.unusedPackages | length' unused-packages.json)

if [ "$unused_count" -gt 0 ]; then
    echo "❌ Found $unused_count unused packages:"
    jq -r '.unusedPackages[] | "- \(.name) v\(.version)"' unused-packages.json
    exit 1
else
    echo "✅ No unused packages found"
fi
```

## 🔍 Troubleshooting

### Common Issues

#### "No projects found"

- Ensure you're pointing to a valid `.csproj` or `.sln` file
- Check that the project file exists and is readable

#### "Access denied"

- Ensure you have read permissions on the project files
- Run with appropriate permissions if necessary

#### Slow performance on large solutions

- Use `--exclude` patterns to skip generated files
- Consider analyzing individual projects instead of the entire solution

### Debug Mode

Use verbose output to diagnose issues:

```bash
nugone analyze --project MySolution.sln --verbose
```

### Known Limitations

- Source generators may create "invisible" dependencies
- Reflection-based usage is not detected
- Some build-time dependencies might be incorrectly flagged

## ❓ Frequently Asked Questions

### Q: Does NuGone modify my project files?

A: No, the `analyze` command only reads and reports. It never modifies files.

### Q: Can NuGone detect runtime dependencies?

A: NuGone detects compile-time dependencies. Runtime dependencies discovered through reflection may not be detected.

### Q: How does NuGone handle transitive dependencies?

A: NuGone focuses on direct package references. Transitive dependencies are not counted as "used" unless directly referenced in code.

### Q: Can I use NuGone in CI/CD pipelines?

A: Yes, NuGone is designed for CI/CD integration with JSON output and exit codes.

### Q: Does NuGone work with .NET Framework projects?

A: NuGone primarily supports SDK-style projects (.NET Core/.NET 5+). Legacy .NET Framework projects have limited support.

### Q: How can I exclude test projects from analysis?

A: Use the exclude configuration or analyze specific projects instead of the entire solution.

### Q: What should I do if NuGone incorrectly flags a package as unused?

A: Add the package to the `excludePackages` configuration or report the issue for investigation.

### Q: Can NuGone detect XAML/WPF dependencies?

A: Basic namespace detection works, but complex XAML bindings might not be fully detected.

### Q: How does NuGone handle conditional compilation?

A: NuGone analyzes all source files but doesn't evaluate conditional compilation directives.

### Q: Can I contribute to NuGone?

A: Yes! See [CONTRIBUTING.md](./CONTRIBUTING.md) for guidelines.

## 📚 Additional Resources

- [Product Requirements Document](./PRD.md) - Detailed feature specifications
- [RFC Documents](./RFCS/) - Technical design decisions
- [GitHub Repository](https://github.com/ahmet-cetinkaya/nugone) - Source code and issues
- [NuGet Package](https://www.nuget.org/packages/NuGone/) - Package information and updates

---

_For questions, bug reports, or feature requests, please visit the [GitHub repository](https://github.com/ahmet-cetinkaya/nugone)._
