using Microsoft.Extensions.DependencyInjection;
using NuGone.Application.Shared.Extensions;
using NuGone.Cli.Features.AnalyzeCommand.Commands;
// using NuGone.Cli.Features.ConfigCommand.Commands;
// using NuGone.Cli.Features.RemoveCommand.Commands;
using NuGone.FileSystem.Extensions;
using NuGone.NuGet.Extensions;
using Spectre.Console.Cli;

// Set up dependency injection
var services = new ServiceCollection();
services.AddLogging();
services.AddApplicationServices();
services.AddFileSystemServices();
services.AddNuGetServices();

// Create the command app with dependency injection
var registrar = new TypeRegistrar(services);
CommandApp app = new(registrar);

app.Configure(config =>
{
    // Configure application metadata
    config.SetApplicationName("nugone");
    config.SetApplicationVersion("1.0.0");

    // Add commands following RFC-0001 structure
    config
        .AddCommand<AnalyzeCommand>("analyze")
        .WithDescription("Analyze project(s) for unused NuGet packages")
        .WithExample(["analyze", "--project", "MySolution.sln"])
        .WithExample(["analyze", "--dry-run", "--format", "json"]);

    // config
    //     .AddCommand<RemoveCommand>("remove")
    //     .WithDescription("Remove unused NuGet packages from project(s)")
    //     .WithExample(["remove", "--project", "MySolution.sln"])
    //     .WithExample(["remove", "--exclude", "System.Text.Json"]);

    // config
    //     .AddCommand<ConfigCommand>("config")
    //     .WithDescription("Manage NuGone configuration settings")
    //     .WithExample(["config", "list"])
    //     .WithExample(["config", "set", "excludeNamespaces", "System.Text.Json"]);
});

return app.Run(args);

// Type registrar for Spectre.Console.Cli dependency injection
public sealed class TypeRegistrar(IServiceCollection builder) : ITypeRegistrar
{
    private readonly IServiceCollection _builder = builder;

    public ITypeResolver Build()
    {
        return new TypeResolver(_builder.BuildServiceProvider());
    }

    public void Register(Type service, Type implementation)
    {
        _builder.AddSingleton(service, implementation);
    }

    public void RegisterInstance(Type service, object implementation)
    {
        _builder.AddSingleton(service, implementation);
    }

    public void RegisterLazy(Type service, Func<object> factory)
    {
        _builder.AddSingleton(service, _ => factory());
    }
}

// Type resolver for Spectre.Console.Cli dependency injection
public sealed class TypeResolver(IServiceProvider provider) : ITypeResolver, IDisposable
{
    private readonly IServiceProvider _provider =
        provider ?? throw new ArgumentNullException(nameof(provider));

    public object? Resolve(Type? type)
    {
        if (type == null)
        {
            return null;
        }

        return _provider.GetService(type);
    }

    public void Dispose()
    {
        if (_provider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
