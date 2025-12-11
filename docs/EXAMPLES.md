# 💡 NuGone Examples

This document provides practical examples of using NuGone in various scenarios and configurations.

## 📋 Table of Contents

1. [Basic Usage Examples](#basic-usage-examples)
2. [Configuration Examples](#configuration-examples)
3. [CI/CD Integration Examples](#cicd-integration-examples)
4. [Advanced Scenarios](#advanced-scenarios)
5. [Sample Project Structures](#sample-project-structures)
6. [Output Examples](#output-examples)

## 🚀 Basic Usage Examples

### Analyzing a Single Project

```bash
# Basic analysis
nugone analyze --project src/MyProject/MyProject.csproj

# With verbose output
nugone analyze --project src/MyProject/MyProject.csproj --verbose

# JSON output
nugone analyze --project src/MyProject/MyProject.csproj --format json
```

### Analyzing a Solution

```bash
# Analyze entire solution
nugone analyze --project MySolution.sln

# Save results to file
nugone analyze --project MySolution.sln --output results.json

# Exclude specific packages
nugone analyze --project MySolution.sln --exclude Newtonsoft.Json --exclude AutoMapper
```

### Working with Output

```bash
# Save JSON for further processing
nugone analyze --project . --format json --output analysis.json

# Pipe JSON to jq for filtering
nugone analyze --project . --format json | jq '.unusedPackages[] | select(.referencesFound == 0)'

# Get count of unused packages
nugone analyze --project . --format json | jq '.unusedPackages | length'
```

## ⚙️ Configuration Examples

### Web API Project Configuration

`global.json` for a typical ASP.NET Core Web API:

```json
{
    "sdk": {
        "version": "9.0.100"
    },
    "nugone": {
        "excludeNamespaces": [
            "Microsoft.AspNetCore.Mvc",
            "Microsoft.Extensions.Logging",
            "Microsoft.EntityFrameworkCore",
            "Swashbuckle.AspNetCore"
        ],
        "excludePackages": [
            "Microsoft.AspNetCore.App",
            "Microsoft.NET.Test.Sdk",
            "coverlet.collector"
        ],
        "excludeFiles": [
            "**/*.Designer.cs",
            "**/obj/**",
            "**/bin/**",
            "**/wwwroot/**"
        ]
    }
}
```

### Desktop Application Configuration

`global.json` for a WPF/WinForms application:

```json
{
    "sdk": {
        "version": "9.0.100"
    },
    "nugone": {
        "excludeNamespaces": [
            "System.Windows",
            "System.Windows.Controls",
            "Microsoft.Xaml"
        ],
        "excludePackages": ["Microsoft.Toolkit.Mvvm", "MaterialDesignThemes"],
        "excludeFiles": [
            "**/*.g.cs",
            "**/*.g.i.cs",
            "**/*.Designer.cs",
            "**/Properties/*.cs"
        ]
    }
}
```

### Library Project Configuration

`global.json` for a NuGet library:

```json
{
    "sdk": {
        "version": "9.0.100"
    },
    "nugone": {
        "excludeNamespaces": [
            "System.Runtime.CompilerServices",
            "System.Diagnostics.CodeAnalysis"
        ],
        "excludePackages": [
            "MinVer",
            "Nerdbank.GitVersioning",
            "Microsoft.SourceLink.GitHub"
        ],
        "excludeFiles": ["**/obj/**", "**/bin/**"]
    }
}
```

## 🔗 CI/CD Integration Examples

### GitHub Actions - Pull Request Check

```yaml
name: Check Unused Packages

on:
  pull_request:
    branches: [ main ]

jobs:
  check-packages:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'

    - name: Cache NuGone
      uses: actions/cache@v3
      with:
        path: ~/.nuget/tools
        key: ${{ runner.os }}-nugone-1.0.0

    - name: Install NuGone
      run: dotnet tool install --global nugone --version 1.0.0

    - name: Analyze packages
      run: |
        nugone analyze --project . --format json --output unused-packages.json

    - name: Comment PR with results
      if: always()
      uses: actions/github-script@v6
      with:
        script: |
          const fs = require('fs');
          if (fs.existsSync('unused-packages.json')) {
            const results = JSON.parse(fs.readFileSync('unused-packages.json', 'utf8'));
            if (results.unusedPackages && results.unusedPackages.length > 0) {
              const packageList = results.unusedPackages
                .map(p => `- ${p.name} v${p.version}`)
                .join('\n');

              const comment = `## 📦 Unused Package Analysis

Found ${results.unusedPackages.length} unused packages:
${packageList}

Consider removing these packages to keep your project clean.`;

              github.rest.issues.createComment({
                issue_number: context.issue.number,
                owner: context.repo.owner,
                repo: context.repo.repo,
                body: comment
              });
            }
          }
```

### Azure DevOps - Build Pipeline

```yaml
trigger:
    - main

variables:
    solution: "**/*.sln"
    buildPlatform: "Any CPU"
    buildConfiguration: "Release"

stages:
    - stage: Build
      displayName: "Build and Analyze"
      jobs:
          - job: Build
            displayName: "Build Job"
            pool:
                vmImage: "windows-latest"

            steps:
                - task: UseDotNet@2
                  displayName: "Use .NET 9.0"
                  inputs:
                      packageType: "sdk"
                      version: "9.x"

                - task: DotNetCoreCLI@2
                  displayName: "Install NuGone"
                  inputs:
                      command: "custom"
                      custom: "tool"
                      arguments: "install --global nugone"

                - task: DotNetCoreCLI@2
                  displayName: "Restore"
                  inputs:
                      command: "restore"
                      projects: "$(solution)"

                - task: DotNetCoreCLI@2
                  displayName: "Build"
                  inputs:
                      command: "build"
                      projects: "$(solution)"
                      arguments: "--configuration $(buildConfiguration) --no-restore"

                - script: |
                      nugone analyze --project $(solution) --format json --output $(Build.ArtifactStagingDirectory)/unused-packages.json
                  displayName: "Analyze unused packages"
                  failOnStderr: true

                - task: PublishBuildArtifacts@1
                  displayName: "Publish analysis results"
                  inputs:
                      pathtoPublish: "$(Build.ArtifactStagingDirectory)/unused-packages.json"
                      artifactName: "package-analysis"
```

### GitLab CI - Merge Request Pipeline

```yaml
stages:
    - analyze
    - build

variables:
    SOLUTION: "*.sln"

analyze_packages:
    stage: analyze
    image: mcr.microsoft.com/dotnet/sdk:9.0
    script:
        - dotnet tool install --global nugone
        - nugone analyze --project $SOLUTION --format json --output unused-packages.json
    artifacts:
        reports:
            junit: unused-packages.json
        paths:
            - unused-packages.json
    only:
        - merge_requests

build:
    stage: build
    image: mcr.microsoft.com/dotnet/sdk:9.0
    script:
        - dotnet restore $SOLUTION
        - dotnet build $SOLUTION --no-restore
    dependencies:
        - analyze_packages
```

## 🔧 Advanced Scenarios

### PowerShell Script for Automated Cleanup

```powershell
# NuGone-Cleanup.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectPath,

    [switch]$AutoRemove,
    [switch]$Backup,
    [string]$ConfigPath = "."
)

# Install NuGone if not present
if (!(Get-Command nugone -ErrorAction SilentlyContinue)) {
    Write-Host "Installing NuGone..." -ForegroundColor Yellow
    dotnet tool install --global nugone
}

# Analyze project
Write-Host "Analyzing project: $ProjectPath" -ForegroundColor Green
$results = nugone analyze --project $ProjectPath --format json | ConvertFrom-Json

if ($results.unusedPackages.Count -eq 0) {
    Write-Host "✅ No unused packages found" -ForegroundColor Green
    exit 0
}

Write-Host "Found $($results.unusedPackages.Count) unused packages:" -ForegroundColor Yellow
$results.unusedPackages | ForEach-Object {
    Write-Host "  - $($_.name) v$($_.version)" -ForegroundColor Cyan
}

if ($AutoRemove) {
    Write-Host "Auto-remove enabled. This feature is planned for future releases." -ForegroundColor Yellow
    Write-Host "For now, please manually remove packages from your .csproj file." -ForegroundColor Yellow
}

# Save results
$outputPath = Join-Path $ConfigPath "unused-packages.json"
$results | ConvertTo-Json -Depth 10 | Out-File -FilePath $outputPath -Encoding UTF8
Write-Host "Results saved to: $outputPath" -ForegroundColor Green
```

### Bash Script with Git Integration

```bash
#!/bin/bash
# nugone-check.sh - Check for unused packages and create a branch if found

set -e

PROJECT_PATH=${1:-.}
BRANCH_NAME="cleanup/unused-packages-$(date +%Y%m%d-%H%M%S)"

# Analyze packages
echo "🔍 Analyzing packages in $PROJECT_PATH..."
nugone analyze --project "$PROJECT_PATH" --format json > unused-packages.json

UNUSED_COUNT=$(jq '.unusedPackages | length' unused-packages.json)

if [ "$UNUSED_COUNT" -eq 0 ]; then
    echo "✅ No unused packages found"
    rm unused-packages.json
    exit 0
fi

echo "📦 Found $UNUSED_COUNT unused packages:"
jq -r '.unusedPackages[] | "  - \(.name) v\(.version)"' unused-packages.json

# Create branch for cleanup (optional)
if [ "$2" = "--create-branch" ]; then
    echo "🌿 Creating cleanup branch: $BRANCH_NAME"
    git checkout -b "$BRANCH_NAME"
    git add unused-packages.json
    git commit -m "docs: add unused packages analysis

Found $UNUSED_COUNT unused packages that may be removed"
    echo "Branch created. Review the packages and remove them manually."
fi
```

### Docker Integration

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS builder

WORKDIR /src

# Copy source code
COPY . .

# Install NuGone
RUN dotnet tool install --global nugone

# Run analysis
RUN nugone analyze --project . --format json --output /app/unused-packages.json

# Runtime stage (optional)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=builder /app/unused-packages.json .
```

### Multi-Project Solution Analysis

```bash
#!/bin/bash
# analyze-solution.sh - Analyze each project separately and create summary

SOLUTION_FILE=${1:-MySolution.sln}
OUTPUT_DIR="analysis-results"

mkdir -p "$OUTPUT_DIR"

# Get list of projects
projects=($(dotnet sln "$SOLUTION_FILE" list | grep '.csproj$'))

echo "📊 Analyzing ${#projects[@]} projects..."

total_unused=0

for project in "${projects[@]}"; do
    project_name=$(basename "$project" .csproj)
    output_file="$OUTPUT_DIR/${project_name}_unused.json"

    echo "🔍 Analyzing $project_name..."
    nugone analyze --project "$project" --format json --output "$output_file" --quiet

    unused_count=$(jq '.unusedPackages | length' "$output_file")
    total_unused=$((total_unused + unused_count))

    if [ "$unused_count" -gt 0 ]; then
        echo "  ❌ $unused_count unused packages"
    else
        echo "  ✅ No unused packages"
    fi
done

echo ""
echo "📈 Summary:"
echo "  Total projects: ${#projects[@]}"
echo "  Total unused packages: $total_unused"
echo "  Results saved to: $OUTPUT_DIR"
```

## 📁 Sample Project Structures

### Web API Project Structure

```
MyWebApi/
├── MyWebApi.sln
├── global.json
├── src/
│   ├── MyWebApi/
│   │   ├── MyWebApi.csproj
│   │   ├── Program.cs
│   │   ├── Controllers/
│   │   ├── Models/
│   │   └── Properties/
│   └── MyWebApi.Tests/
│       ├── MyWebApi.Tests.csproj
│       └── Tests/
└── Directory.Packages.props
```

### Typical `global.json` for Web API

```json
{
    "sdk": {
        "version": "9.0.100"
    },
    "nugone": {
        "excludeNamespaces": [
            "Microsoft.AspNetCore.Mvc",
            "Microsoft.Extensions.Logging",
            "Microsoft.EntityFrameworkCore",
            "Swashbuckle.AspNetCore",
            "xUnit"
        ],
        "excludePackages": [
            "Microsoft.AspNetCore.App",
            "Microsoft.NET.Test.Sdk",
            "coverlet.collector",
            "Microsoft.AspNetCore.Mvc.Testing"
        ]
    }
}
```

## 📄 Output Examples

### Text Output Example

```
🔍 Analyzing project: MyProject.csproj
📦 Total packages: 25
✅ Used packages: 22
❌ Unused packages: 3

Unused Packages:
  - Newtonsoft.Json v13.0.3
  - AutoMapper v12.0.1
  - Serilog.Sinks.File v5.0.0

Analysis completed in 1.2 seconds
```

### JSON Output Example

```json
{
    "project": "MyProject.csproj",
    "scannedPackages": 25,
    "unusedPackages": [
        {
            "name": "Newtonsoft.Json",
            "version": "13.0.3",
            "referencesFound": 0,
            "frameworks": ["net8.0"]
        },
        {
            "name": "AutoMapper",
            "version": "12.0.1",
            "referencesFound": 0,
            "frameworks": ["net8.0"]
        },
        {
            "name": "Serilog.Sinks.File",
            "version": "5.0.0",
            "referencesFound": 0,
            "frameworks": ["net8.0"]
        }
    ],
    "usedPackages": 22,
    "analysisTime": "00:00:01.234",
    "timestamp": "2025-12-06T10:30:00Z"
}
```

### Solution Analysis JSON Example

```json
{
    "solution": "MySolution.sln",
    "projects": [
        {
            "name": "MyProject.csproj",
            "scannedPackages": 25,
            "unusedPackages": 3,
            "usedPackages": 22
        },
        {
            "name": "MyProject.Tests.csproj",
            "scannedPackages": 15,
            "unusedPackages": 1,
            "usedPackages": 14
        }
    ],
    "totalScannedPackages": 40,
    "totalUnusedPackages": 4,
    "totalUsedPackages": 36,
    "analysisTime": "00:00:03.456",
    "timestamp": "2025-12-06T10:30:00Z"
}
```

## 🔍 Debug Examples

### Verbose Output Example

```bash
nugone analyze --project MyProject.csproj --verbose
```

Output:

```
🔍 Starting analysis with verbose output
📁 Project: MyProject.csproj
🎯 Target Frameworks: net8.0
📦 Found 25 package references

🔍 Scanning source files...
  ✓ Scanning Controllers/UserController.cs
  ✓ Scanning Models/User.cs
  ✓ Scanning Services/UserService.cs
  ✓ Scanning 15 files total

🔍 Analyzing package usage...
  ✓ Newtonsoft.Json: 0 references
  ✓ Microsoft.Extensions.DependencyInjection: 5 references
  ✓ Microsoft.EntityFrameworkCore: 3 references
  ...
  ✓ xUnit: 8 references

📊 Analysis complete:
  Used packages: 22
  Unused packages: 3
  Analysis time: 1.2 seconds
```

## 🛠️ Custom Extensions

### Custom Filter Script (Python)

```python
#!/usr/bin/env python3
# filter_unused.py - Filter NuGone results based on custom criteria

import json
import sys
from datetime import datetime

def load_results(filename):
    with open(filename, 'r') as f:
        return json.load(f)

def filter_packages(results, min_version=None, exclude_patterns=None):
    filtered = []
    for pkg in results.get('unusedPackages', []):
        # Filter by version
        if min_version:
            pkg_version = pkg.get('version', '0.0.0')
            if pkg_version < min_version:
                continue

        # Filter by name patterns
        if exclude_patterns:
            if any(pattern in pkg.get('name', '') for pattern in exclude_patterns):
                continue

        filtered.append(pkg)

    return filtered

def main():
    if len(sys.argv) < 2:
        print("Usage: python filter_unused.py <results.json> [min_version]")
        sys.exit(1)

    results_file = sys.argv[1]
    min_version = sys.argv[2] if len(sys.argv) > 2 else None
    exclude_patterns = ['Test', 'Mock']  # Exclude test and mock packages

    results = load_results(results_file)
    filtered = filter_packages(results, min_version, exclude_patterns)

    output = {
        'timestamp': datetime.now().isoformat(),
        'originalResults': results,
        'filteredUnusedPackages': filtered,
        'filteredCount': len(filtered)
    }

    print(json.dumps(output, indent=2))

if __name__ == '__main__':
    main()
```

---

_For more examples and community contributions, visit the [GitHub repository](https://github.com/ahmet-cetinkaya/nugone)._
