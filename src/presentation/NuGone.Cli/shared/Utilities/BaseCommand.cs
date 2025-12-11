#nullable disable
#pragma warning disable CA1716 // Allow 'Shared' namespace
#pragma warning disable CA1040 // Allow empty interface marker
using NuGone.Cli.Shared.Models;
using Spectre.Console.Cli;

namespace NuGone.Cli.Shared.Utilities;

/// <summary>
/// Marker interface for async commands.
/// </summary>
public interface IAsyncCommand<TSettings>
    where TSettings : CommandSettings { }

/// <summary>
/// Base class for all NuGone CLI commands.
/// Implements RFC-0001: CLI Architecture And Command Design with Result/Error pattern.
/// Provides common functionality and error handling patterns.
/// </summary>
public abstract class BaseCommand<TSettings> : Command<TSettings>
    where TSettings : CommandSettings
{
    /// <summary>
    /// Executes the command with standardized error handling using Result pattern.
    /// </summary>
    public sealed override int Execute(
        CommandContext context,
        TSettings settings,
        CancellationToken cancellationToken
    )
    {
        return GlobalExceptionHandler.ExecuteWithGlobalHandler(
            () =>
            {
                // Check if this is an async command
                if (this is IAsyncCommand<TSettings>)
                {
                    var asyncResult = ExecuteCommandAsync(context, settings)
                        .GetAwaiter()
                        .GetResult();
                    return asyncResult.Match(
                        onSuccess: exitCode => exitCode,
                        onFailure: error =>
                        {
                            DisplayError(error);
                            return error.ExitCode;
                        }
                    );
                }
                else
                {
                    var result = ExecuteCommand(context, settings);
                    return result.Match(
                        onSuccess: exitCode => exitCode,
                        onFailure: error =>
                        {
                            DisplayError(error);
                            return error.ExitCode;
                        }
                    );
                }
            },
            IsVerboseMode(settings)
        );
    }

    /// <summary>
    /// Executes the specific command logic using Result pattern. Override this method in derived classes.
    /// </summary>
    protected virtual Result<int> ExecuteCommand(CommandContext context, TSettings settings)
    {
        throw new NotImplementedException(
            "Either ExecuteCommand or ExecuteCommandAsync must be implemented"
        );
    }

    /// <summary>
    /// Executes the specific command logic asynchronously using Result pattern. Override this method in async derived classes.
    /// Note: CancellationToken is managed internally by the base command for consistent behavior across sync and async commands.
    /// </summary>
    protected virtual Task<Result<int>> ExecuteCommandAsync(
        CommandContext context,
        TSettings settings
    )
    {
        throw new NotImplementedException(
            "Either ExecuteCommand or ExecuteCommandAsync must be implemented"
        );
    }

    /// <summary>
    /// Validates the project path and returns the resolved path using Result pattern.
    /// </summary>
    protected static Result<string> ValidateAndResolveProjectPath(string projectPath)
    {
        if (string.IsNullOrEmpty(projectPath))
        {
            projectPath = Directory.GetCurrentDirectory();
            ConsoleHelpers.WriteInfo(
                $"No project specified, using current directory: {projectPath}"
            );
        }

        if (!Directory.Exists(projectPath) && !File.Exists(projectPath))
        {
            return Error.InvalidArgument(
                $"Project path does not exist: {projectPath}",
                "projectPath"
            );
        }

        try
        {
            var fullPath = Path.GetFullPath(projectPath);
            return Result<string>.Success(fullPath);
        }
        catch (ArgumentException ex)
        {
            return Error.InvalidArgument($"Invalid project path: {ex.Message}", "projectPath");
        }
        catch (NotSupportedException ex)
        {
            return Error.InvalidArgument(
                $"Unsupported project path format: {ex.Message}",
                "projectPath"
            );
        }
    }

    /// <summary>
    /// Validates command settings and returns any validation errors.
    /// </summary>
    protected virtual Result ValidateSettings(TSettings settings)
    {
        // Default implementation - no validation errors
        return Result.Success();
    }

    /// <summary>
    /// Displays error information to the user.
    /// </summary>
    private static void DisplayError(Error error)
    {
        ConsoleHelpers.WriteError(error.Message);

        if (IsVerboseMode(null) && error.Details.Count > 0)
        {
            ConsoleHelpers.WriteVerbose("Error Details:");
            foreach (var detail in error.Details)
            {
                ConsoleHelpers.WriteVerbose($"  {detail.Key}: {detail.Value}");
            }
        }
    }

    /// <summary>
    /// Checks if verbose mode is enabled (if the settings support it).
    /// </summary>
    protected static bool IsVerboseMode(TSettings settings)
    {
        if (settings == null)
            return false;

        // Use reflection to check for a Verbose property
        var verboseProperty = typeof(TSettings).GetProperty("Verbose");
        return verboseProperty?.GetValue(settings) as bool? ?? false;
    }
}
