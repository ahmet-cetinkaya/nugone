# Version Compatibility

This document outlines NuGone's version compatibility with .NET runtimes and operating systems.

## .NET Support

| NuGone Version | Minimum .NET | Supported Versions |
| -------------- | ------------ | ------------------ |
| 1.0            | .NET 9.0     | 9.0 only           |
| 1.1            | .NET 9.0     | 9.0 only           |
| 2.0.x          | .NET 9.0     | 9.0 only           |
| 2.1.x+         | .NET 8.0     | 8.0, 9.0, 10.0+    |

## Platform Support

All versions support:

- **Windows**: 10 and later
- **macOS**: 10.15 and later
- **Linux**: Most modern distributions

## SDK Requirements

| NuGone Version | Minimum SDK  |
| -------------- | ------------ |
| 1.0            | .NET 9.0 SDK |
| 1.1            | .NET 9.0 SDK |
| 2.0.x          | .NET 9.0 SDK |
| 2.1.x+         | .NET 8.0 SDK |

## Key Changes by Version

### v2.0.x → v2.1.x

- **Improvement**: Now supports .NET 8.0 (downgrade from 9.0 requirement)
- Added multi-target support for .NET 8, 9, and 10
- Enhanced central package management support with recursive imports
- Added comprehensive cross-platform path compatibility improvements
- Upgraded project dependencies including Microsoft.Extensions packages to v10.0.1
- Removed MediatR dependency for simplified architecture

### v2.1.0 Latest Features

- **New**: Multi-target .NET support (8.0, 9.0, 10.0) with Directory.Build.props
- **New**: Comprehensive central package management (CPM) support with recursive imports
- **New**: Version bump automation script with conventional commit parsing
- **New**: LoggerMessage source generator for high-performance structured logging
- **New**: Known build packages whitelist to prevent false positives
- **Enhanced**: Cross-platform path compatibility for Windows and Unix systems
- **Enhanced**: Test coverage with comprehensive domain entity tests
- **Improved**: Code quality with ConfigureAwait(false) throughout async codebase
- **Updated**: Project dependencies to latest stable versions

## Upgrade Guide

### From v2.0.x to v2.1.x

No configuration changes needed. Just ensure you have .NET 8.0+ SDK installed.

### From v1.x to v2.1.x

1. Install .NET 8.0+ SDK
2. Update configuration from `nugone.json` to `global.json`
3. Install latest version: `dotnet tool install nugone --global`

## Feature Compatibility

| Feature                    | v1.0 | v1.1 | v2.0.x | v2.1.0+        |
| -------------------------- | ---- | ---- | ------ | -------------- |
| Package Analysis           | ✅   | ✅   | ✅     | ✅             |
| JSON Output                | ✅   | ✅   | ✅     | ✅             |
| Global Using Detection     | ❌   | ✅   | ✅     | ✅             |
| Package Removal            | ✅   | ✅   | ❌     | Planned v2.2.0 |
| Central Package Management | ❌   | ❌   | ❌     | ✅ (v2.1.0)    |
| .NET 8+ Support            | ❌   | ❌   | ❌     | ✅             |
| CancellationToken Support  | ❌   | ❌   | ✅     | ✅             |
| Multi-target Support       | ❌   | ❌   | ❌     | ✅             |
| Recursive CPM Imports      | ❌   | ❌   | ❌     | ✅ (v2.1.0)    |
| Build Packages Whitelist   | ❌   | ❌   | ❌     | ✅ (v2.1.0)    |
| Cross-platform Paths       | ❌   | ❌   | ❌     | ✅ (v2.1.0)    |
| High-performance Logging   | ❌   | ❌   | ❌     | ✅ (v2.1.0)    |
| Version Automation Scripts | ❌   | ❌   | ❌     | ✅ (v2.1.0)    |
