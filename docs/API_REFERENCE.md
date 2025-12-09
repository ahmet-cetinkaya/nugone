# API Reference

This document provides reference information for extending NuGone's functionality through its public APIs and extension points.

## Table of Contents

- [Overview](#overview)
- [Core Interfaces](#core-interfaces)
- [Domain Entities](#domain-entities)
- [CLI Extension Points](#cli-extension-points)
- [Extension Examples](#extension-examples)
- [Dependency Injection](#dependency-injection)

## Overview

NuGone follows Clean Architecture principles with clear separation of concerns. The public APIs are designed around interfaces that allow for customization and extension while maintaining the core functionality.

Key architectural layers:

- **Presentation**: CLI interface using Spectre.Console.Cli
- **Application**: Business logic with CQRS pattern
- **Domain**: Core entities and business rules
- **Infrastructure**: External integrations (file system, NuGet)

## Core Interfaces

### IPackageUsageAnalyzer

The main interface for analyzing package usage in .NET projects.

```csharp
namespace NuGone.Application.Services;

public interface IPackageUsageAnalyzer
{
    Task<AnalyzePackageUsageResult> AnalyzeAsync(
        AnalyzePackageUsageCommand command,
        CancellationToken cancellationToken = default);
}
```

**Usage**: Implement this interface to create custom package analysis logic.

**Key Methods**:

- `AnalyzeAsync()`: Analyzes packages in a solution or project

### Repository Interfaces

#### ISolutionRepository

```csharp
namespace NuGone.Application.Repositories;

public interface ISolutionRepository
{
    Task<Solution?> GetSolutionAsync(string solutionPath, CancellationToken cancellationToken = default);
}
```

#### IProjectRepository

```csharp
namespace NuGone.Application.Repositories;

public interface IProjectRepository
{
    Task<IReadOnlyList<Project>> GetProjectsAsync(
        Solution solution,
        CancellationToken cancellationToken = default);
}
```

#### INuGetRepository

```csharp
namespace NuGone.Application.Repositories;

public interface INuGetRepository
{
    Task<IReadOnlyList<string>> GetNamespacesAsync(
        string packageName,
        CancellationToken cancellationToken = default);
}
```

### Command/Handler Pattern

#### AnalyzePackageUsageCommand

```csharp
namespace NuGone.Application.Commands;

public record AnalyzePackageUsageCommand(
    string SolutionPath,
    string[] ExcludePatterns = null,
    string OutputFormat = "text",
    bool Verbose = false) : ICommand<AnalyzePackageUsageResult>;
```

#### AnalyzePackageUsageHandler

```csharp
namespace NuGone.Application.Commands;

public class AnalyzePackageUsageHandler
    : ICommandHandler<AnalyzePackageUsageCommand, AnalyzePackageUsageResult>
{
    public Task<AnalyzePackageUsageResult> HandleAsync(
        AnalyzePackageUsageCommand command,
        CancellationToken cancellationToken = default)
    {
        // Implementation
    }
}
```

## Domain Entities

### Solution

```csharp
namespace NuGone.Domain.Entities;

public class Solution
{
    public string Path { get; init; } = string.Empty;
    public IReadOnlyList<Project> Projects { get; init; } = Array.Empty<Project>();
    public bool HasCentralPackageManagement { get; init; }
}
```

### Project

```csharp
namespace NuGone.Domain.Entities;

public class Project
{
    public string Name { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public IReadOnlyList<string> TargetFrameworks { get; init; } = Array.Empty<string>();
    public IReadOnlyList<PackageReference> PackageReferences { get; init; } = Array.Empty<PackageReference>();
    public IReadOnlyList<string> SourceFiles { get; init; } = Array.Empty<string>();
}
```

### PackageReference

```csharp
namespace NuGone.Domain.Entities;

public class PackageReference
{
    public string Name { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public bool IsUsed { get; init; }
    public IReadOnlyList<UsageLocation> UsageLocations { get; init; } = Array.Empty<UsageLocation>();
}
```

### GlobalUsing

```csharp
namespace NuGone.Domain.Entities;

public class GlobalUsing
{
    public string Namespace { get; init; } = string.Empty;
    public string Alias { get; init; } = string.Empty;
    public bool IsStatic { get; init; }
}
```

## CLI Extension Points

### BaseCommand

Base class for creating custom CLI commands.

```csharp
namespace NuGone.Cli.Shared.Utilities;

public abstract class BaseCommand<TSettings, TResult> : Command<TSettings>
    where TSettings : CommandSettings
    where TResult : notnull
{
    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        TSettings settings)
    {
        // Setup dependency injection
        var services = CreateServiceProvider();
        var handler = services.GetRequiredService<ICommandHandler<TSettings, TResult>>();

        // Execute command
        var result = await handler.HandleAsync(settings, context.CancellationToken);

        // Handle result
        return await HandleResultAsync(result, settings);
    }

    protected virtual IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        return services.BuildServiceProvider();
    }

    protected abstract void ConfigureServices(IServiceCollection services);
    protected abstract Task<int> HandleResultAsync(TResult result, TSettings settings);
}
```

### Type Registration

#### ITypeRegistrar

```csharp
namespace NuGone.Cli.Shared.Registration;

public interface ITypeRegistrar
{
    void Register(Type service, Type implementation);
    void RegisterInstance(Type service, object implementation);
    void RegisterSingleton(Type service, Type implementation);
    void RegisterTransient(Type service, Type implementation);
    void RegisterLazy(Type service, Func<object> factory);
}
```

#### ITypeResolver

```csharp
namespace NuGone.Cli.Shared.Registration;

public interface ITypeResolver
{
    object Resolve(Type type);
    T Resolve<T>() where T : class;
    bool TryResolve(Type type, out object? resolved);
    bool TryResolve<T>(out T? resolved) where T : class;
}
```

## Extension Examples

### Creating a Custom CLI Command

```csharp
using NuGone.Cli.Shared.Utilities;
using Spectre.Console.Cli;

// Settings class for your command
public class CustomCommandSettings : CommandSettings
{
    [CommandArgument(0, "<path>")]
    public required string Path { get; set; }

    [CommandOption("-v|--verbose")]
    public bool Verbose { get; set; }
}

// Result class
public class CustomResult
{
    public int ProcessedCount { get; init; }
    public IReadOnlyList<string> Messages { get; init; } = Array.Empty<string>();
}

// Handler class
public class CustomCommandHandler : ICommandHandler<CustomCommandSettings, CustomResult>
{
    public Task<CustomResult> HandleAsync(CustomCommandSettings settings, CancellationToken cancellationToken)
    {
        // Your custom logic here
        return Task.FromResult(new CustomResult
        {
            ProcessedCount = 42,
            Messages = new[] { "Processing complete" }
        });
    }
}

// Command class
public class CustomCommand : BaseCommand<CustomCommandSettings, CustomResult>
{
    public override int Execute(CommandContext context, CustomCommandSettings settings)
    {
        return base.Execute(context, settings);
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ICommandHandler<CustomCommandSettings, CustomResult>, CustomCommandHandler>();
        // Add other services as needed
    }

    protected override Task<int> HandleResultAsync(CustomResult result, CustomCommandSettings settings)
    {
        if (settings.Verbose)
        {
            Console.WriteLine($"Processed {result.ProcessedCount} items");
        }

        foreach (var message in result.Messages)
        {
            Console.WriteLine(message);
        }

        return Task.FromResult(0);
    }
}
```

### Adding a Custom Analyzer

```csharp
using NuGone.Application.Services;
using NuGone.Application.Commands;

public class CustomPackageAnalyzer : IPackageUsageAnalyzer
{
    public async Task<AnalyzePackageUsageResult> AnalyzeAsync(
        AnalyzePackageUsageCommand command,
        CancellationToken cancellationToken = default)
    {
        // Your custom analysis logic
        var unusedPackages = new List<PackageReference>();

        // Example: Check for specific patterns
        if (command.SolutionPath.EndsWith(".csproj"))
        {
            // Single project analysis
            var project = await AnalyzeProjectAsync(command.SolutionPath, cancellationToken);
            unusedPackages.AddRange(project.PackageReferences.Where(p => !p.IsUsed));
        }

        return new AnalyzePackageUsageResult
        {
            UnusedPackages = unusedPackages,
            AnalysisTime = DateTime.UtcNow
        };
    }

    private async Task<Project> AnalyzeProjectAsync(string projectPath, CancellationToken cancellationToken)
    {
        // Your project analysis implementation
        // Read .csproj, analyze source files, determine package usage
        return new Project
        {
            Name = Path.GetFileNameWithoutExtension(projectPath),
            Path = projectPath,
            TargetFrameworks = new[] { "net8.0" },
            PackageReferences = new List<PackageReference>(),
            SourceFiles = Array.Empty<string>()
        };
    }
}
```

### Implementing a Custom Repository

```csharp
using NuGone.Application.Repositories;
using NuGone.Domain.Entities;

public class CustomProjectRepository : IProjectRepository
{
    public async Task<IReadOnlyList<Project>> GetProjectsAsync(
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        var projects = new List<Project>();

        // Custom logic to discover projects
        if (solution.HasCentralPackageManagement)
        {
            // Handle central package management
            projects = await GetProjectsFromDirectoryPropsAsync(solution.Path, cancellationToken);
        }
        else
        {
            // Handle standard project references
            projects = await GetProjectsFromSolutionAsync(solution.Path, cancellationToken);
        }

        return projects;
    }

    private async Task<List<Project>> GetProjectsFromDirectoryPropsAsync(
        string solutionPath,
        CancellationToken cancellationToken)
    {
        // Implementation for reading Directory.Packages.props
        // and discovering all projects in the solution
        return new List<Project>();
    }

    private async Task<List<Project>> GetProjectsFromSolutionAsync(
        string solutionPath,
        CancellationToken cancellationToken)
    {
        // Implementation for parsing .slnx files
        // and extracting project references
        return new List<Project>();
    }
}
```

## Dependency Injection

NuGone uses Microsoft.Extensions.DependencyInjection for dependency injection. When creating custom commands or extensions, you can register your services in the `ConfigureServices` method.

### Service Registration Patterns

```csharp
protected override void ConfigureServices(IServiceCollection services)
{
    // Register handlers
    services.AddSingleton<ICommandHandler<CustomCommandSettings, CustomResult>, CustomCommandHandler>();

    // Register analyzers
    services.AddSingleton<IPackageUsageAnalyzer, CustomPackageAnalyzer>();

    // Register repositories
    services.AddSingleton<IProjectRepository, CustomProjectRepository>();

    // Register existing services if needed
    services.AddSingleton<ISolutionRepository, SolutionRepository>();
    services.AddSingleton<INuGetRepository, NuGetRepository>();
}
```

### Service Lifetimes

- **Singleton**: Use for stateless services that can be shared
- **Transient**: Use for stateful services that need new instances
- **Scoped**: Not commonly used in CLI context but available

## Integration Points

### With Existing NuGone Services

When extending NuGone, you can leverage existing services:

```csharp
public class ExtendedCommandHandler : ICommandHandler<CustomCommandSettings, CustomResult>
{
    private readonly ISolutionRepository _solutionRepository;
    private readonly IPackageUsageAnalyzer _analyzer;

    public ExtendedCommandHandler(
        ISolutionRepository solutionRepository,
        IPackageUsageAnalyzer analyzer)
    {
        _solutionRepository = solutionRepository;
        _analyzer = analyzer;
    }

    public async Task<CustomResult> HandleAsync(CustomCommandSettings settings, CancellationToken cancellationToken)
    {
        // Use existing services
        var solution = await _solutionRepository.GetSolutionAsync(settings.Path, cancellationToken);

        // Extend with custom logic
        // ...
    }
}
```

### Configuration

NuGone configuration is stored in `global.json`. You can access it:

```csharp
public class ConfigurationAwareCommandHandler
{
    public async Task<CustomResult> HandleAsync(CustomCommandSettings settings, CancellationToken cancellationToken)
    {
        var globalJsonPath = Path.Combine(settings.Path, "global.json");

        if (File.Exists(globalJsonPath))
        {
            var config = await File.ReadAllTextAsync(globalJsonPath, cancellationToken);
            // Parse configuration
        }

        // Continue with processing
    }
}
```

## Related Documentation

- [STRUCTURE.md](STRUCTURE.md) - Architecture overview
- [RFC-0001](RFCS/RFC-0001-CLI-ARCHITECTURE-AND-COMMAND-DESIGN.md) - CLI design
- [RFC-0002](RFCS/RFC-0002-UNUSED-PACKAGE-DETECTION-ALGORITHM.md) - Algorithm details
- [CONTRIBUTING.md](CONTRIBUTING.md) - Development setup
