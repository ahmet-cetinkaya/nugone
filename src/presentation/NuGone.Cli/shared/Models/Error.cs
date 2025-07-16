using NuGone.Cli.Shared.Constants;

namespace NuGone.Cli.Shared.Models;

/// <summary>
/// Represents an expected error condition in the application.
/// Used for predictable failures that should not throw exceptions.
/// </summary>
public record Error
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public int ExitCode { get; init; }
    public Dictionary<string, object> Details { get; init; } = new();

    public static Error InvalidArgument(string message, string? argumentName = null)
    {
        var details = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(argumentName))
            details["ArgumentName"] = argumentName;

        return new Error
        {
            Code = "INVALID_ARGUMENT",
            Message = message,
            ExitCode = ExitCodes.InvalidArgument,
            Details = details,
        };
    }

    public static Error FileNotFound(string filePath)
    {
        return new Error
        {
            Code = "FILE_NOT_FOUND",
            Message = $"File not found: {filePath}",
            ExitCode = ExitCodes.FileNotFound,
            Details = new Dictionary<string, object> { ["FilePath"] = filePath },
        };
    }

    public static Error InvalidFileFormat(string message, string extension)
    {
        return new Error
        {
            Code = "INVALID_FILE_FORMAT",
            Message = $"{message}: {extension}",
            ExitCode = ExitCodes.InvalidFileFormat,
            Details = new Dictionary<string, object> { ["Extension"] = extension },
        };
    }

    public static Error DirectoryNotFound(string directoryPath)
    {
        return new Error
        {
            Code = "DIRECTORY_NOT_FOUND",
            Message = $"Directory not found: {directoryPath}",
            ExitCode = ExitCodes.DirectoryNotFound,
            Details = new Dictionary<string, object> { ["DirectoryPath"] = directoryPath },
        };
    }

    public static Error AccessDenied(string resource)
    {
        return new Error
        {
            Code = "ACCESS_DENIED",
            Message = $"Access denied: {resource}",
            ExitCode = ExitCodes.AccessDenied,
            Details = new Dictionary<string, object> { ["Resource"] = resource },
        };
    }

    public static Error ValidationFailed(string message, Dictionary<string, object>? details = null)
    {
        return new Error
        {
            Code = "VALIDATION_FAILED",
            Message = message,
            ExitCode = ExitCodes.InvalidArgument,
            Details = details ?? new Dictionary<string, object>(),
        };
    }

    public static Error OperationFailed(string operation, string reason)
    {
        return new Error
        {
            Code = "OPERATION_FAILED",
            Message = $"Operation '{operation}' failed: {reason}",
            ExitCode = ExitCodes.OperationFailed,
            Details = new Dictionary<string, object>
            {
                ["Operation"] = operation,
                ["Reason"] = reason,
            },
        };
    }

    public static Error Custom(
        string code,
        string message,
        int exitCode,
        Dictionary<string, object>? details = null
    )
    {
        return new Error
        {
            Code = code,
            Message = message,
            ExitCode = exitCode,
            Details = details ?? new Dictionary<string, object>(),
        };
    }
}
