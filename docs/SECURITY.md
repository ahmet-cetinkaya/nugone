# Security

This document outlines NuGone's security model, threat analysis, and security controls.

## Table of Contents

- [Security Model](#security-model)
- [Threat Analysis](#threat-analysis)
- [Security Controls](#security-controls)
- [Dependency Security](#dependency-security)
- [Code Security Practices](#code-security-practices)
- [Reporting Security Issues](#reporting-security-issues)

## Security Model

NuGone is a .NET global tool that analyzes NuGet package usage in local .NET projects. Its security model focuses on safe file access and input validation while operating with user permissions.

### Security Scope

**In Scope**:

- File system access within user-provided project directories
- Processing of .NET project files (.csproj, .slnx)
- Reading NuGet package metadata
- Writing analysis output (console, JSON files)

**Out of Scope**:

- Network communications (except for NuGet package metadata)
- System-level modifications
- User authentication or authorization
- Encryption of data at rest or in transit

### Trust Boundaries

```
User (provides project paths)
    ↓
NuGone CLI Application
    ↓
File System (.csproj, source files)
    ↓
NuGet.org (read-only package metadata)
```

## Threat Analysis

### File System Risks

#### Path Traversal

- **Threat**: Malicious paths like `../../../etc/passwd` could access files outside project
- **Impact**: Information disclosure, potential system file modification
- **Mitigation**: Path validation, canonicalization, and access control checks

#### File Access Violations

- **Threat**: Accessing files without proper permissions
- **Impact**: Unauthorized read access, potential crashes
- **Mitigation**: Graceful error handling, permission checks

#### Resource Exhaustion

- **Threat**: Processing extremely large projects or files
- **Impact**: Denial of service, memory exhaustion
- **Mitigation**: Cancellation tokens, streaming processing

### Package Injection Risks

#### Malicious Package Metadata

- **Threat**: Tampered or malicious package information from NuGet.org
- **Impact**: Incorrect analysis results, potential security guidance issues
- **Mitigation**: HTTPS communication, package signature validation (future)

#### Dependency Confusion

- **Threat**: Internal package names published to public NuGet
- **Impact**: Wrong packages being analyzed, supply chain issues
- **Mitigation**: Package source validation, explicit package sources

### Information Disclosure

#### Sensitive File Exposure

- **Threat**: Accidental inclusion of sensitive files in analysis
- **Impact**: Exposure of credentials, keys, or proprietary code
- **Mitigation**: Default exclusion patterns, configurable exclusions

#### Error Message Leakage

- **Threat**: Error messages revealing internal paths or system information
- **Impact**: Information useful to attackers
- **Mitigation**: Sanitized error messages, verbose mode flag

## Security Controls

### Input Validation

#### Path Validation

```csharp
// Example of safe path handling
public static string ValidatePath(string path, string basePath)
{
    // Canonicalize path
    var fullPath = Path.GetFullPath(path);

    // Ensure it's within the allowed base path
    if (!fullPath.StartsWith(Path.GetFullPath(basePath)))
    {
        throw new UnauthorizedAccessException("Path traversal detected");
    }

    return fullPath;
}
```

#### Project File Validation

- Only processes known file types (.csproj, .slnx, .cs, .vb)
- Validates XML structure before parsing
- Limits file size to prevent resource exhaustion

### Error Handling

#### Safe Error Messages

```csharp
// Avoid exposing internal details
catch (UnauthorizedAccessException ex)
{
    // Generic message for user
    Console.WriteLine("Access denied: Check file permissions");

    // Detailed logging for debugging (verbose mode only)
    if (verbose)
    {
        Console.WriteLine($"Details: {ex.Message}");
    }
}
```

#### Exception Handling Strategy

- Specific catch blocks for different exception types
- No exception details exposed in normal mode
- Verbose mode available for debugging with user consent

### File Access Controls

#### Access Pattern

- Read-only access to user-provided directories
- No modifications to user files
- Optional write access to user-specified output locations

#### Isolation

- Operates within user permissions
- No elevation of privileges
- System.IO.Abstractions layer for testing and control

## Dependency Security

### Secure Dependencies

NuGone uses the following security-focused dependencies:

#### Microsoft.CodeAnalysis.Analyzers

- **Purpose**: Static code analysis
- **Security**: Built-in security analyzers
- **Version**: Latest stable with security patches

#### System.IO.Abstractions

- **Purpose**: File system abstraction
- **Security**: Testable file operations, prevents accidental system calls
- **Version**: Actively maintained with security updates

#### Microsoft.Extensions Packages

- **Purpose**: Logging and dependency injection
- **Security**: Official Microsoft packages with security review
- **Version**: Matches .NET 8/9/10 security releases

### Vulnerability Management

#### Automated Scanning

- GitHub Dependabot integration (recommended)
- Weekly vulnerability scans
- Automated PRs for dependency updates

#### Manual Review

- Security-focused review for new dependencies
- Assessment of dependency trustworthiness
- Alternative evaluation for high-risk packages

### Supply Chain Security

#### Package Sources

- Primary: NuGet.org (official, HTTPS)
- Secondary: Configured private feeds (user-defined)
- Blocked: Unauthenticated or HTTP-only sources

#### Build Security

- Signed builds (future enhancement)
- Reproducible builds documentation
- Source code provenance tracking

## Code Security Practices

### Defensive Programming

#### Null Safety

- Nullable reference types enabled
- Null checks in all public methods
- Guard clauses for input validation

```csharp
public string AnalyzeProject(string projectPath)
{
    if (string.IsNullOrEmpty(projectPath))
        throw new ArgumentNullException(nameof(projectPath));

    if (!File.Exists(projectPath))
        throw new FileNotFoundException("Project file not found", projectPath);

    // Continue with analysis
}
```

#### Regex Security

- Compiled regex for performance and security
- Timeout protection for regex operations
- Input validation before regex matching

```csharp
[GeneratedRegex(@"^[a-zA-Z0-9._-]+$", RegexOptions.Compiled, TimeSpan.FromSeconds(1))]
private static partial Regex SafePackageNameRegex();
```

### Performance Security

#### Cancellation Support

All long-running operations support cancellation:

```csharp
public async Task<AnalysisResult> AnalyzeAsync(
    string projectPath,
    CancellationToken cancellationToken = default)
{
    cancellationToken.ThrowIfCancellationRequested();

    // Perform analysis with cancellation checks
}
```

#### Memory Management

- Streaming processing for large files
- Dispose pattern for file handles
- No long-lived object retention

### Logging Security

#### No Sensitive Data in Logs

- No file contents in logs
- No package contents in logs
- Sanitized paths in error messages

#### Configurable Verbosity

```csharp
if (verbose)
{
    // Detailed logging for debugging
    _logger.LogDebug("Processing file: {FileName}", fileName);
}
else
{
    // Minimal information in normal mode
    Console.WriteLine("Processing files...");
}
```

## Reporting Security Issues

### Responsible Disclosure

We follow responsible disclosure principles for security vulnerabilities.

#### What to Report

- Security vulnerabilities
- Security design concerns
- Potential security improvements
- Security-related bugs

#### What NOT to Report

- General bugs or feature requests (use GitHub Issues)
- Security questions (use GitHub Discussions)
- Third-party vulnerabilities (report to respective projects)

### Reporting Process

#### Primary Method: GitHub Security Advisories

1. **Go to**: [GitHub Security Advisories](https://github.com/ahmet-cetinkaya/nugone/security/advisories)
2. **Click**: "Report a vulnerability"
3. **Provide**:
    - Detailed description of the vulnerability
    - Steps to reproduce
    - Potential impact
    - Proof of concept (if available)

#### Email (Optional)

For sensitive issues that cannot be reported via GitHub:

- Use GitHub's private vulnerability reporting feature
- Do NOT send unencrypted details via email

### Response Timeline

- **Initial Response**: Within 7 days
- **Detailed Assessment**: Within 14 days
- **Fix Timeline**: Based on severity
    - Critical: 7 days
    - High: 14 days
    - Medium: 30 days
    - Low: Next release

### Security Updates

Security updates will be:

1. Fixed in the development branch
2. Released as patch versions
3. Clearly marked in CHANGELOG.md
4. Available through standard update channels

### Recognition

Security researchers who report vulnerabilities will be:

- Acknowledged in release notes (with permission)
- Listed in our Security Hall of Fame
- Eligible for NuGone merchandise (future)

## Security Best Practices for Users

### Running NuGone Safely

1. **Review Project Paths**: Ensure you're analyzing the correct project
2. **Use Appropriate Permissions**: Run with minimum necessary privileges
3. **Check Outputs**: Review analysis results before taking action
4. **Keep Updated**: Use latest version for security patches

### Enterprise Security

1. **Scan Before Use**: Run through security scanners
2. **Isolate Execution**: Consider running in sandboxed environment
3. **Audit Usage**: Monitor execution in CI/CD pipelines
4. **Policy Compliance**: Ensure compliance with organizational security policies

## Related Documentation

- [RFC-0004: Package Removal and Safety Mechanisms](RFCS/RFC-0004-PACKAGE-REMOVAL-AND-SAFETY-MECHANISMS.md)
- [CONTRIBUTING.md](CONTRIBUTING.md) - Security in development
- [DEPLOYMENT.md](DEPLOYMENT.md) - Secure deployment practices
