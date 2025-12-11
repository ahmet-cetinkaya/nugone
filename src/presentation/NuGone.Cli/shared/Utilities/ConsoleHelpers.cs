using Spectre.Console;

namespace NuGone.Cli.Shared.Utilities;

/// <summary>
/// Shared console utilities and helpers for the CLI.
/// Implements RFC-0001: CLI Architecture And Command Design.
/// Provides cross-platform console output with proper error handling.
/// </summary>
internal static class ConsoleHelpers
{
    public static void WriteSuccess(string message)
    {
        AnsiConsole.MarkupLine($"[green]✓[/] {EscapeMarkup(message)}");
    }

    public static void WriteError(string message)
    {
        AnsiConsole.MarkupLine($"[red]✗[/] {EscapeMarkup(message)}");
    }

    public static void WriteWarning(string message)
    {
        AnsiConsole.MarkupLine($"[yellow]⚠[/] {EscapeMarkup(message)}");
    }

    public static void WriteInfo(string message)
    {
        AnsiConsole.MarkupLine($"[blue]ℹ[/] {EscapeMarkup(message)}");
    }

    public static void WriteVerbose(string message)
    {
        AnsiConsole.MarkupLine($"[dim]🔍 {EscapeMarkup(message)}[/]");
    }

    public static bool Confirm(string message, bool defaultValue = false)
    {
        return AnsiConsole.Confirm(message, defaultValue);
    }

    public static void WriteTable<T>(
        IEnumerable<T> items,
        params (string Header, Func<T, string> Selector)[] columns
    )
    {
        var table = new Table();

        foreach (var (header, _) in columns)
        {
            _ = table.AddColumn(header);
        }

        foreach (var item in items)
        {
            var values = columns.Select(col => EscapeMarkup(col.Selector(item))).ToArray();
            _ = table.AddRow(values);
        }

        AnsiConsole.Write(table);
    }

    public static void WriteRule(string title)
    {
        AnsiConsole.Write(new Rule(title).RuleStyle("grey"));
    }

    /// <summary>
    /// Escapes markup characters to prevent injection and ensure cross-platform compatibility.
    /// </summary>
    private static string EscapeMarkup(string text)
    {
        return text?.Replace("[", "[[", StringComparison.Ordinal)
                .Replace("]", "]]", StringComparison.Ordinal) ?? string.Empty;
    }

    /// <summary>
    /// Writes a progress indicator for long-running operations.
    /// </summary>
    public static async Task WithProgress(
        string description,
        Func<IProgress<string>, Task> operation
    )
    {
        await AnsiConsole
            .Progress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask(description);
                var progress = new Progress<string>(status =>
                {
                    task.Description = status;
                    task.Increment(1);
                });

                await operation(progress).ConfigureAwait(false);
                task.Value = 100;
            })
            .ConfigureAwait(false);
    }
}
