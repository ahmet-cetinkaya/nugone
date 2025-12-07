using Microsoft.Extensions.DependencyInjection;
using NuGone.Application.Shared.Extensions;
using NuGone.Cli.Features.AnalyzeCommand.Commands;
using NuGone.Cli.Infrastructure;
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
    _ = config.SetApplicationName("nugone");
    _ = config.SetApplicationVersion("2.0.1");

    // Add commands following RFC-0001 structure
    _ = config
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
