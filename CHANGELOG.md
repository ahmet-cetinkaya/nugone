# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

## [Unreleased]

### Added

- (No new features added yet)

### Changed

- (No changes yet)

### Removed

- (Nothing removed yet)

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

[2.0.0]: https://github.com/ahmet-cetinkaya/nugone/compare/v1.1.0...v2.0.0
[1.1.0]: https://github.com/ahmet-cetinkaya/nugone/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/ahmet-cetinkaya/nugone/releases/tag/v1.0.0
