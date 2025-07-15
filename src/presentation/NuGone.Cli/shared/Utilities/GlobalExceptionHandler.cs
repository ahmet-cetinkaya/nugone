using NuGone.Cli.Shared.Constants;

namespace NuGone.Cli.Shared.Utilities;

/// <summary>
/// Global exception handler for unexpected system-level errors.
/// Handles exceptions that represent truly unexpected situations.
/// </summary>
public static class GlobalExceptionHandler
{
    /// <summary>
    /// Handles unexpected exceptions and converts them to appropriate exit codes.
    /// </summary>
    public static int HandleUnexpectedException(Exception exception, bool verbose = false)
    {
        // Log the exception details for debugging
        ConsoleHelpers.WriteError("An unexpected error occurred");

        if (verbose)
        {
            ConsoleHelpers.WriteVerbose($"Exception Type: {exception.GetType().Name}");
            ConsoleHelpers.WriteVerbose($"Message: {exception.Message}");
            ConsoleHelpers.WriteVerbose($"Stack Trace: {exception.StackTrace}");

            if (exception.InnerException != null)
            {
                ConsoleHelpers.WriteVerbose(
                    $"Inner Exception: {exception.InnerException.GetType().Name}"
                );
                ConsoleHelpers.WriteVerbose($"Inner Message: {exception.InnerException.Message}");
            }
        }
        else
        {
            ConsoleHelpers.WriteError($"Error: {exception.Message}");
            ConsoleHelpers.WriteInfo("Use --verbose flag for detailed error information");
        }

        // Map specific system exceptions to appropriate exit codes
        return exception switch
        {
            // Critical system exceptions (most specific first)
            OutOfMemoryException => ExitCodes.UnexpectedError,
            StackOverflowException => ExitCodes.UnexpectedError,
            AccessViolationException => ExitCodes.UnexpectedError,

            // File system exceptions that weren't caught and converted to Error
            UnauthorizedAccessException => ExitCodes.AccessDenied,
            DirectoryNotFoundException => ExitCodes.DirectoryNotFound,
            FileNotFoundException => ExitCodes.FileNotFound,

            // Argument exceptions that weren't caught and converted to Error (most specific first)
            ArgumentNullException => ExitCodes.InvalidArgument,
            ArgumentOutOfRangeException => ExitCodes.InvalidArgument,
            ArgumentException => ExitCodes.InvalidArgument,

            // Other system exceptions (most specific first)
            PlatformNotSupportedException => ExitCodes.UnexpectedError,
            NotSupportedException => ExitCodes.UnexpectedError,
            InvalidOperationException => ExitCodes.UnexpectedError,

            // Default for any other unexpected exception
            _ => ExitCodes.UnexpectedError,
        };
    }

    /// <summary>
    /// Wraps command execution with global exception handling.
    /// </summary>
    public static int ExecuteWithGlobalHandler(Func<int> commandExecution, bool verbose = false)
    {
        try
        {
            return commandExecution();
        }
        catch (Exception ex)
        {
            return HandleUnexpectedException(ex, verbose);
        }
    }

    /// <summary>
    /// Wraps async command execution with global exception handling.
    /// </summary>
    public static async Task<int> ExecuteWithGlobalHandlerAsync(
        Func<Task<int>> commandExecution,
        bool verbose = false
    )
    {
        try
        {
            return await commandExecution();
        }
        catch (Exception ex)
        {
            return HandleUnexpectedException(ex, verbose);
        }
    }
}
