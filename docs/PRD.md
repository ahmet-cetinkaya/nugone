# 📄 NuGone Product Requirements Document (PRD)

## 1. Purpose

To automatically detect and optionally remove unused NuGet package references in .NET projects.

### Problem Statement

* Developers frequently add new NuGet packages during development.
* Over time, some packages become unused but remain referenced in project files:
  * Increasing project size
  * Slowing build times
  * Introducing potential security risks
* Neither `dotnet` CLI nor Visual Studio fully automates unused package removal.

### Goal

* Identify unused NuGet packages.
* Optionally remove them from project files.
* Offer this functionality as a cross-platform .NET CLI tool.

---

## 2. Scope

### 2.1 Supported Platforms

* .NET Core 3.1+
* .NET 5+
* .NET 6+
* .NET 7+
* .NET 8+
* .NET 9+
* Cross-platform CLI (Windows, macOS, Linux)

---

### 2.2 Supported Project Types

* SDK-style .NET projects (e.g. *.csproj*)
* Multi-project solutions (*.sln*, *.slnx*)

---

## 3. Features

### 3.1 Package Detection

- Read all `<PackageReference>` entries from project files and central props files.
- Distinguish between direct and transitive dependencies.
- Handle conditional references (e.g., `<Condition>` attributes).
- For each package:
  - Scan codebase for:
    - `using` statements referring to the package’s namespaces.
    - Class or method names from the package used in code.
    - (Planned) Reflection-based usage detection.
  - Support multi-targeted projects.
  - Exclude files/folders by pattern (e.g., `**/Generated/**`).

### 3.2 Reporting

- Output a report listing:
  - Used packages
  - Unused packages
  - Summary (total unused, total scanned, percentage unused)
- Output formats:
  - Plain text
  - JSON
- Option to save report to file (`--output <file>`)

**Example JSON Output:**

```json
{
  "unusedPackages": [
    {
      "name": "Newtonsoft.Json",
      "version": "13.0.3",
      "referencesFound": 0
    }
  ],
  "scannedPackages": 10,
  "project": "MyProject.csproj"
}
```

### 3.3 Removal Functionality

- CLI command to remove packages:
  - Remove a single unused package: `dotnet nuget-unused remove --package <PackageName>`
  - Remove all unused packages: `dotnet nuget-unused remove --all-unused`
- Prompt for confirmation before removal (unless `--yes` is specified).
- Create a backup of the project file before removal (`--backup`).
- Attempt to rebuild the project after removal (unless `--no-build`).
- Report any build errors to the user and roll back changes if removal fails.

### 3.4 Dry-Run Support

- Allow previewing unused packages without actually removing them: `dotnet nuget-unused analyze --dry-run`
- No files are modified in dry-run mode.

### 3.5 Performance & Configuration

- For large solutions:
  - Parallelized code scanning.
  - Exclude certain files (e.g., .Designer.cs, .g.cs) for speed.
- Configuration support:
  - Read configuration options from `global.json` at the solution root (preferred method).
  - Tüm NuGone konfigürasyonları `nugone` ana objesi altında toplanır.
  - Exclude namespaces, files, or folders via `nugone` objesi (`global.json` içinde).
  - Fallback to legacy config file (JSON) only if `global.json` is not present.
  - Specify config file location and precedence (global.json > legacy config > defaults).

```json
{
  "nugone": {
    "excludeNamespaces": [
      "System.Text.Json"
    ],
    "excludeFiles": [
      "**/*.Designer.cs",
      "**/Generated/**"
    ]
  }
}
```

---

## 4. CLI Commands

### Analyze

```bash
nugone analyze [options]
```

| Flag             | Description                      |
| ---------------- | -------------------------------- |
| --dry-run        | Only list packages, don’t remove |
| --format <type>  | Output format: `json` or `text`  |
| --exclude <name> | Exclude packages from analysis   |
| --output <file>  | Write report to file             |
| --project <path> | Path to project or solution      |
| --help           | Show help                        |

### Remove

```bash
nugone remove [options]
```

| Flag             | Description                        |
| ---------------- | ---------------------------------- |
| --package <name> | Remove a single unused package     |
| --all-unused     | Remove all unused packages         |
| --no-build       | Skip build after removal           |
| --yes, -y        | Skip confirmation prompt           |
| --backup         | Backup project file before removal |
| --help           | Show help                          |

### Config (Planned)

```bash
nugone config [options]
```

| Flag                | Description               |
| ------------------- | ------------------------- |
| --set <key> <value> | Set a configuration value |
| --get <key>         | Get a configuration value |
| --help              | Show help                 |

---

## 5. Limitations

- Reflection-based usage detection is complex → deferred to a future release.
- Source generators may introduce invisible dependencies → can result in false positives.
- Transitive dependencies require additional analysis logic.

---

## 6. User Stories

> **As a developer,** I want to detect unused NuGet packages in my projects so that I can keep my projects lean and maintainable.

> **As a DevOps engineer,** I want to integrate unused package analysis into my CI pipelines to keep build artifacts minimal and secure.

---

## 7. Performance Targets

- For solutions with 5,000+ files:
  - Complete analysis under 2 minutes on modern hardware.
- Target memory overhead: < 50 MB additional memory usage during analysis.
- Parallelized scanning for improved performance.

---

## 8. Security Considerations

- Validate and sanitize all CLI arguments and config file inputs.
- Avoid executing user code during analysis.
- Create backups before modifying project files.
- Fail gracefully and provide informative error messages.

---

## 9. Testing Strategy

- Unit tests for core logic (package detection, removal, reporting).
- Integration tests with sample projects and solutions.
- Regression tests for edge cases (multi-targeting, conditional references, central package management).
- Automated CI integration for all supported platforms.

---

## 10. Logging & Diagnostics

- Verbose and debug logging options (`--verbosity <level>`).
- Colorized CLI output for warnings/errors.
- Progress indicators for long-running operations.
- Clear error messages and exit codes for CI integration.

---

## 12. License

- MIT License recommended → encourages community contributions.

---

## 13. Roadmap

### V1

- Package detection
- Code scanning (namespace + class names)
- CLI reporting

### V2

- Dry-run support
- Single-package removal
- Config file support

### V3

- Source generator support
- Transitive dependency analysis
- Reflection-based analysis

---

## Summary

This tool aims to fill a significant gap by automatically detecting and optionally removing unused NuGet packages from .NET projects, helping keep projects lightweight, faster, and more secure.
