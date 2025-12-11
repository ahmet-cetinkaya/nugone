using System.ComponentModel;
using NuGone.Application.Features.PackageAnalysis.Services.Abstractions;
using NuGone.Cli.Shared.Constants;
using NuGone.Cli.Shared.Models;
using NuGone.Cli.Shared.Utilities;
using Spectre.Console.Cli;

namespace NuGone.Cli.Features.ConfigCommand.Commands;

/// <summary>
/// CLI command for managing NuGone configuration.
/// Implements RFC-0001: CLI Architecture And Command Design.
/// Future implementation planned.
/// </summary>
public class ConfigCommand : BaseCommand<ConfigCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("Configuration action: get, set, list")]
        [CommandArgument(0, "[action]")]
        public string? Action { get; init; }

        [Description("Configuration key")]
        [CommandArgument(1, "[key]")]
        public string? Key { get; init; }

        [Description("Configuration value")]
        [CommandArgument(2, "[value]")]
        public string? Value { get; init; }

        [Description("Global configuration (affects all projects)")]
        [CommandOption("--global")]
        public bool Global { get; init; }
    }

    protected override Result<int> ExecuteCommand(CommandContext context, Settings settings)
    {
        // Validate settings first
        var settingsValidation = ValidateConfigSettings(settings);
        if (!settingsValidation.IsValid)
            return Error.ValidationFailed(string.Join(", ", settingsValidation.Errors));

        ConsoleHelpers.WriteWarning("Configuration management is planned for a future release");
        ConsoleHelpers.WriteInfo("This command will allow you to:");
        ConsoleHelpers.WriteInfo("  - Configure exclude patterns");
        ConsoleHelpers.WriteInfo("  - Set default output formats");
        ConsoleHelpers.WriteInfo("  - Manage global.json integration");

        return ExitCodes.Success;
    }

    public static ValidationResult ValidateConfigSettings(Settings settings)
    {
        var errors = new List<string>();

        if (!string.IsNullOrEmpty(settings.Action))
        {
            var validActions = new[] { "GET", "SET", "LIST", "RESET" };
            if (!validActions.Contains(settings.Action.ToUpperInvariant()))
            {
                errors.Add(
                    $"Action must be one of: {string.Join(", ", validActions)}. Provided: {settings.Action}"
                );
            }

            if (
                string.Equals(settings.Action, "set", StringComparison.OrdinalIgnoreCase)
                && (string.IsNullOrEmpty(settings.Key) || string.IsNullOrEmpty(settings.Value))
            )
            {
                errors.Add("Both key and value are required for 'set' action");
            }
        }

        return errors.Count > 0 ? ValidationResult.Failure(errors) : ValidationResult.Success();
    }
}
