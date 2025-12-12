# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.1.0] - 2025-12-12

### Added

- Multi-target .NET support (8.0, 9.0, 10.0) with Directory.Build.props
- Comprehensive central package management (CPM) support with recursive imports
- Version bump automation script with conventional commit parsing and changelog generation
- LoggerMessage source generator for high-performance structured logging
- Comprehensive test suite for domain entities and value objects
- Automated version management with git integration and safety checkpoints
- Known build packages whitelist to prevent false positives in unused package detection
- Multi-framework validation support in test scripts for .NET 8.0, 9.0, and 10.0
- Cross-platform path compatibility improvements for Windows and Unix systems

### Changed

- Upgraded project dependencies to latest versions:
    - Microsoft.Extensions packages to v10.0.1
    - Spectre.Console.Cli to v0.53.1
    - System.IO.Abstractions to v22.1.0
    - Microsoft.CodeAnalysis.NetAnalyzers to v10.0.101
- Migrated test assertions from FluentAssertions to Shouldly for improved readability
- Refactored codebase with ConfigureAwait(false) throughout async methods
- Enhanced null argument validation using ArgumentNullException.ThrowIfNull
- Improved string comparison operations with StringComparison.Ordinal/InvariantCulture
- Updated .NET analyzer configurations with targeted suppressions
- Enhanced CLI command classes to use sealed types
- Refactored repositories to use partial classes for logging separation
- Replaced legacy .sln solution file with modern .slnx XML-based format
- Added InternalsVisibleTo attributes to core project assemblies for test accessibility
- Updated GitHub Actions to test across multiple .NET versions

### Fixed

- Cross-platform path compatibility issues in solution repository
- CancellationToken parameter inconsistency in BaseCommand.ExecuteCommandAsync
- LoggerMessage source generator pattern from instance-based to static pattern
- XML namespace handling in SLNX file parsing
- MockFileSystem GetDirectoryName issue on Unix with Windows paths
- Relative path resolution with .. and . navigation
- Lint script error handling for package restore failures
- Missing package version logging verification
- RollForward configuration for .NET 10 compatibility
- Windows drive-rooted path handling for CPM detection

### Deprecated

- MediatR dependency removed from application layer

### CI/CD

- Added .NET 10.0.x support to test matrix
- Enhanced version bump script with dry-run mode and changelog preview
- Improved error handling in build scripts with fail-fast behavior
- Added markdownlint configuration and integrated into lint pipeline
- Migrated cspell configuration to dedicated JSON file
- Updated global.json to enforce consistent SDK version (9.0.306)
- Added common output functions for consistent colored logging across scripts
- Enhanced test script with multi-framework validation and verbose output

### Documentation

- Added comprehensive documentation index and user guides
- Created API reference, architecture decisions, deployment, and security documentation
- Updated .NET SDK version references to 8.0 in documentation
- Added EXAMPLES.md with practical usage scenarios and CI/CD integration
- Added USER_GUIDE.md with installation, commands, and troubleshooting sections
- Translated Turkish text to English in PRD for consistency

### BREAKING CHANGE

- Removed MediatR dependency from application layer
- Minimum .NET version requirement updated to 8.0

## [2.0.1] - 2025-12-06

### Added

- Central package management (CPM) support with Directory.Packages.props detection
- .NET 10 compatibility support through RollForward Major setting

### Changed

- Updated project documentation and banner images

### Fixed

- Fixed version display consistency between CLI and package metadata
- Corrected central package management detection edge cases
- Improved error handling in package version resolution

## [2.0.0] - 2025-10-31

### Added

- CancellationToken support to CLI commands for better cancellation support
- Comprehensive test suite for DotnetToolSettings.xml configuration
- Roslynator tool configuration for enhanced code analysis
- Shell script formatting support using shfmt
- Test script for compilation checks

### Changed

- Update NuGet packages to latest versions (MediatR, Microsoft.Extensions.\*, System.IO.Abstractions, Spectre.Console.Cli, etc.)
- Refactor validation methods to be public and use ValidationResult pattern consistently
- Improve code organization by extracting TypeRegistrar and TypeResolver into Infrastructure namespace
- Enhance CLI command validation and error handling
- Optimize regex patterns using GeneratedRegex for better performance
- Improve string handling using single-character literals instead of string comparisons
- Update formatting commands to use `dotnet csharpier format` instead of `dotnet-csharpier`

### Removed

- Nothing removed yet

### Fixed

- Fix critical global tool installation by adding missing DotnetToolSettings.xml
- Correct EntryPoint from "NuGone.Cli.dll" to "nugone.dll"
- Fix markdown file references for local viewing using relative paths
- Resolve lint issues and improve code organization

### CI/CD

- Enhance release workflow with tag input support for manual triggering
- Add comprehensive lint script with multiple analysis tools
- Update formatting script with improved commands and shfmt support
- Simplify roslynator step in lint script

### BREAKING CHANGE

- Move TypeRegistrar and TypeResolver to NuGone.Cli.Infrastructure namespace

## [1.1.0] - 2025-07-16

### Added

- Global using detection in analyzer.
- Support for global using declarations in package analysis.

### Changed

- Update version to 1.1.0.

### Fixed

- Fix Roslynator issues.

### CI/CD

- Update .NET Test workflow for consistency.
- Setup .NET 9.0 SDK in NuGet release workflow.
- Update GitHub Actions workflow for release process.

[2.1.0]: https://github.com/ahmet-cetinkaya/nugone/compare/v2.0.1...v2.1.0
[2.0.1]: https://github.com/ahmet-cetinkaya/nugone/compare/v2.0.0...v2.0.1
[2.0.0]: https://github.com/ahmet-cetinkaya/nugone/compare/v1.1.0...v2.0.0
[1.1.0]: https://github.com/ahmet-cetinkaya/nugone/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/ahmet-cetinkaya/nugone/releases/tag/v1.0.0
