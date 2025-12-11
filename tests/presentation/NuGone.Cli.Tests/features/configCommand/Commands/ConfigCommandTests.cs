using NuGone.Application.Features.PackageAnalysis.Services.Abstractions;
using NuGone.Cli.Features.ConfigCommand.Commands;
using NuGone.Cli.Shared.Constants;
using NuGone.Cli.Shared.Models;
using NuGone.Cli.Shared.Utilities;
using Spectre.Console.Cli;

namespace NuGone.Cli.Tests.Commands;

/// <summary>
/// Tests for ConfigCommand class.
/// Validates RFC-0001: CLI Architecture And Command Design - ConfigCommand implementation.
/// </summary>
public partial class ConfigCommandTests
{
    #region Helper Classes

    /// <summary>
    /// Testable version of ConfigCommand that exposes protected methods for testing.
    /// </summary>
    private sealed class TestableConfigCommand : ConfigCommand
    {
        public static ValidationResult TestValidateConfigSettings(ConfigCommand.Settings settings)
        {
            return ConfigCommand.ValidateConfigSettings(settings);
        }

        // Override to prevent actual execution during tests
        protected override Result<int> ExecuteCommand(
            CommandContext context,
            ConfigCommand.Settings settings
        )
        {
            return ExitCodes.Success;
        }
    }

    /// <summary>
    /// Helper class for creating CommandContext
    /// </summary>
    private sealed class FakeRemainingArguments : IRemainingArguments
    {
        public IReadOnlyList<string> Raw => Array.Empty<string>();
        public ILookup<string, string?> Parsed =>
            Array.Empty<string>().ToLookup(x => x, x => (string?)null);
    }

    #endregion
}
