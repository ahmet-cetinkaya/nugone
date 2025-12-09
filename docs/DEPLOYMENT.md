# Deployment Guide

This guide covers building, packaging, and deploying NuGone to various environments.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Build Process](#build-process)
- [Packaging](#packaging)
- [Version Management](#version-management)
- [CI/CD Pipeline](#cicd-pipeline)
- [Publishing to NuGet.org](#publishing-to-nugetorg)
- [Local Testing](#local-testing)
- [Troubleshooting](#troubleshooting)

## Prerequisites

### Development Environment

- **.NET SDK 8.0 or later** - Required for building the project
- **Git** - For source control management
- **GitHub CLI** - For release automation (recommended)

### Required Tools

```bash
# Install CSharpier for code formatting
dotnet tool install -g csharpier

# Install Roslynator for static analysis
dotnet tool restore

# Optional: Install .NET local tool for testing
dotnet tool install --local --add-source ./nupkg nugone
```

## Build Process

### Building the Solution

NuGone uses an XML-based solution format (`.slnx`) instead of traditional `.sln` files.

```bash
# Build in Debug mode
dotnet build NuGone.slnx

# Build in Release mode
dotnet build NuGone.slnx -c Release

# Build with specific target framework
dotnet build NuGone.slnx -f net8.0
dotnet build NuGone.slnx -f net9.0
dotnet build NuGone.slnx -f net10.0
```

### Build Configuration

The project uses `Directory.Build.props` for shared configuration:

```xml
<Project>
  <PropertyGroup>
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisMode>All</AnalysisMode>
  </PropertyGroup>
</Project>
```

### Quality Checks

Run the full quality check suite before deployment:

```bash
# Run all linting checks (formatting, markdownlint, Roslynator, gitleaks)
./scripts/lint.sh

# Run tests
dotnet test NuGone.slnx

# Run test compilation
./scripts/test.sh
```

## Packaging

### Creating NuGet Package

```bash
# Create package in release configuration
dotnet pack src/presentation/NuGone.Cli/NuGone.Cli.csproj -c Release

# Create package with output to specific directory
dotnet pack src/presentation/NuGone.Cli/NuGone.Cli.csproj -c Release -o ./nupkg
```

### Package Configuration

The CLI project is configured as a .NET tool:

```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <PackAsTool>true</PackAsTool>
  <ToolCommandName>nugone</ToolCommandName>
  <PackageId>NuGone</PackageId>
  <PackageProjectUrl>https://github.com/ahmet-cetinkaya/nugone</PackageProjectUrl>
  <RepositoryUrl>https://github.com/ahmet-cetinkaya/nugone</RepositoryUrl>
  <License>GPL-3.0-or-later</License>
  <Copyright>© 2024 Ahmet Çetinkaya</Copyright>
</PropertyGroup>
```

### Package Contents

The generated `.nupkg` includes:

- Compiled binaries for all target frameworks
- Dependencies declared in the project file
- Tool manifest for CLI usage

## Version Management

### Semantic Versioning

NuGone follows [Semantic Versioning](https://semver.org/) (MAJOR.MINOR.PATCH).

- **MAJOR**: Breaking changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes (backward compatible)

### Version Configuration

Version is managed in the project file:

```xml
<PropertyGroup>
  <Version>3.0.0</Version>
  <AssemblyVersion>3.0.0.0</AssemblyVersion>
  <FileVersion>3.0.0.0</FileVersion>
  <PackageVersion>3.0.0</PackageVersion>
</PropertyGroup>
```

### Release Tags

Use Git tags for releases:

```bash
# Create and push a new release tag
git tag v3.0.0
git push origin v3.0.0
```

## CI/CD Pipeline

### GitHub Actions Workflows

#### 1. .NET Test Workflow (`.github/workflows/dotnet-test.yml`)

Triggers on:

- Push to main branch
- Pull requests
- Tag pushes (releases)

Test Matrix:

- .NET 8.0
- .NET 9.0
- Ubuntu, Windows, macOS

```yaml
strategy:
    matrix:
        os: [ubuntu-latest, windows-latest, macos-latest]
        dotnet: ["8.0.x", "9.0.x"]
```

#### 2. GitHub Release Workflow (`.github/workflows/github-release.yml`)

Steps:

1. Validates tag format
2. Builds release artifacts
3. Creates GitHub release
4. Attaches `.nupkg` and tar.gz artifacts

#### 3. NuGet Publish Workflow (`.github/workflows/nuget-release.yml`)

Triggers on:

- GitHub release publication

Publishes to NuGet.org with:

- Automatic API key retrieval from secrets
- Skip duplicate versions
- Validation of package contents

### Local CI/CD Testing

```bash
# Run test workflow locally
act -j test

# Run release workflow locally (requires .env file)
act -j release -s GITHUB_TOKEN=your_token
```

## Publishing to NuGet.org

### Prerequisites

1. **NuGet Account**: Register at [nuget.org](https://www.nuget.org/)
2. **API Key**: Generate an API key in your NuGet account settings
3. **GitHub Secret**: Store the API key as a repository secret (`NUGET_API_KEY`)

### Manual Publishing

```bash
# Install dotnet nuget tool if not already installed
dotnet tool install --global dotnet-nuget

# Configure API key (one-time setup)
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
dotnet nuget setapikey YOUR_API_KEY -s nuget.org

# Push to NuGet.org
dotnet nuget push ./nupkg/NuGone.3.0.0.nupkg -s nuget.org --skip-duplicate
```

### Automated Publishing

The CI/CD pipeline handles publishing automatically when a GitHub release is created:

1. Tag the release: `git tag v3.0.0`
2. Push the tag: `git push origin v3.0.0`
3. GitHub Actions creates the release
4. NuGet workflow publishes to nuget.org

### Package Validation

Before publishing, ensure:

```bash
# Verify package contents
dotnet nuget verify ./nupkg/NuGone.3.0.0.nupkg

# Test package locally
dotnet tool install --local --add-source ./nupkg nugone --version 3.0.0
nugone --version
```

## Local Testing

### Installing as Local Tool

```bash
# Create local manifest file
dotnet new tool-manifest

# Install from local package
dotnet tool install nugone --local --add-source ./nupkg

# Run locally installed tool
dotnet nugone --help
```

### Installing as Global Tool

```bash
# Install from local package
dotnet tool install nugone --global --add-source ./nupkg

# Or install from NuGet.org (if published)
dotnet tool install nugone --global

# Run global tool
nugone --help
```

### Testing Different Target Frameworks

```bash
# Test with specific runtime
nugone --info  # Shows .NET runtime info

# Test on different platforms
# The tool is cross-platform (.NET 8/9/10 support Windows, macOS, Linux)
```

## Troubleshooting

### Common Build Issues

#### "MSB4057: The target "XYZ" does not exist in the project"

**Solution**: Ensure you have .NET SDK 8.0 or later installed.

#### "The command 'dotnet' is not recognized"

**Solution**: Add .NET SDK to your PATH or use the .NET SDK installer.

#### "Package restore failed"

**Solution**: Clear NuGet cache:

```bash
dotnet nuget locals all --clear
dotnet restore
```

### Common Packaging Issues

#### "PackAsTool is not supported in this project type"

**Solution**: Ensure the project is an executable project with `<OutputType>Exe</OutputType>`.

#### "Invalid version format"

**Solution**: Use semantic versioning format (e.g., 1.2.3, 1.2.3-preview.4).

### Common Publishing Issues

#### "401 Unauthorized" from NuGet.org

**Solution**:

1. Verify your API key is correct
2. Ensure the key has "Push" scope
3. Check if the package ID is already taken

#### "Package with same ID and version already exists"

**Solution**:

- Increment version number
- Or use `--skip-duplicate` flag if republishing

#### "Symbol package push failed"

**Solution**: Symbols are optional. Disable with:

```bash
dotnet pack /p:IncludeSymbols=false
```

### CI/CD Issues

#### "Workflow permission denied"

**Solution**:

1. Go to repository Settings > Actions > General
2. Enable "Allow GitHub Actions to create and approve pull requests"

#### "Secret not found"

**Solution**: Add required secrets in repository Settings > Secrets and variables > Actions

### Debugging Tips

1. **Verbose Logging**: Add `--verbosity detailed` to dotnet commands
2. **Clean Build**: Use `dotnet clean` before building
3. **Dependency Graph**: Check with `dotnet list package --outdated`
4. **Target Framework**: Verify with `dotnet --list-runtimes`

## Related Documentation

- [CONTRIBUTING.md](CONTRIBUTING.md) - Development setup
- [CHANGELOG.md](../CHANGELOG.md) - Version history
- [VERSION_COMPATIBILITY.md](VERSION_COMPATIBILITY.md) - .NET version support
