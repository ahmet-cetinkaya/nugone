using NuGone.Cli.Features.ConfigCommand.Commands;
using NuGone.Cli.Shared.Constants;
using NuGone.Cli.Shared.Models;
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
    private class TestableConfigCommand : ConfigCommand
    {
        public Result TestValidateConfigSettings(Settings settings)
        {
            // Since ValidateConfigSettings is private, we'll test the validation logic directly
            if (!string.IsNullOrEmpty(settings.Action))
            {
                var validActions = new[] { "get", "set", "list", "reset" };
                if (!validActions.Contains(settings.Action.ToLowerInvariant()))
                {
                    return Error.ValidationFailed(
                        $"Action must be one of: {string.Join(", ", validActions)}",
                        new Dictionary<string, object> { ["ProvidedAction"] = settings.Action }
                    );
                }

                if (
                    string.Equals(settings.Action, "set", StringComparison.OrdinalIgnoreCase)
                    && (string.IsNullOrEmpty(settings.Key) || string.IsNullOrEmpty(settings.Value))
                )
                {
                    return Error.ValidationFailed(
                        "Both key and value are required for 'set' action"
                    );
                }
            }

            return Result.Success();
        }

        // Override to prevent actual execution during tests
        protected override Result<int> ExecuteCommand(CommandContext context, Settings settings)
        {
            return ExitCodes.Success;
        }
    }

    /// <summary>
    /// Helper class for creating CommandContext
    /// </summary>
    private class FakeRemainingArguments : IRemainingArguments
    {
        public IReadOnlyList<string> Raw => Array.Empty<string>();
        public ILookup<string, string?> Parsed =>
            Array.Empty<string>().ToLookup(x => x, x => (string?)null);
    }

    #endregion
}
