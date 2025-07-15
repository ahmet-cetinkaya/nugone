using NuGone.Cli.Features.AnalyzeCommand.Commands;
using NuGone.Cli.Features.ConfigCommand.Commands;
using NuGone.Cli.Features.RemoveCommand.Commands;
using Spectre.Console.Cli;

CommandApp app = new();

app.Configure(config =>
{
    // Configure application metadata
    config.SetApplicationName("nugone");
    config.SetApplicationVersion("0.1.0");

    // Add commands following RFC-0001 structure
    config
        .AddCommand<AnalyzeCommand>("analyze")
        .WithDescription("Analyze project(s) for unused NuGet packages")
        .WithExample(new[] { "analyze", "--project", "MySolution.sln" })
        .WithExample(new[] { "analyze", "--dry-run", "--format", "json" });

    config
        .AddCommand<RemoveCommand>("remove")
        .WithDescription("Remove unused NuGet packages from project(s)")
        .WithExample(new[] { "remove", "--project", "MySolution.sln" })
        .WithExample(new[] { "remove", "--exclude", "System.Text.Json" });

    config
        .AddCommand<ConfigCommand>("config")
        .WithDescription("Manage NuGone configuration settings")
        .WithExample(new[] { "config", "list" })
        .WithExample(new[] { "config", "set", "excludeNamespaces", "System.Text.Json" });
});

return app.Run(args);
