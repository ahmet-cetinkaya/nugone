namespace NuGone.Domain.Shared.ValueObjects;

/// <summary>
/// Represents an error that occurred during an operation.
/// Used with the Result pattern for error handling without exceptions.
/// </summary>
public class Error(string code, string message) : IEquatable<Error>
{
    /// <summary>
    /// The error code that identifies the type of error.
    /// </summary>
    public string Code { get; } = code ?? throw new ArgumentNullException(nameof(code));

    /// <summary>
    /// The human-readable error message.
    /// </summary>
    public string Message { get; } = message ?? throw new ArgumentNullException(nameof(message));

    /// <summary>
    /// Creates an error with the specified code and message.
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="message">The error message</param>
    /// <returns>A new error instance</returns>
    public static Error Create(string code, string message) => new(code, message);

    /// <summary>
    /// Creates an error with a generic code and the specified message.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <returns>A new error instance</returns>
    public static Error Create(string message) => new("GENERAL_ERROR", message);

    /// <summary>
    /// Creates a validation error.
    /// </summary>
    /// <param name="message">The validation error message</param>
    /// <returns>A new validation error</returns>
    public static Error Validation(string message) => new("VALIDATION_ERROR", message);

    /// <summary>
    /// Creates a not found error.
    /// </summary>
    /// <param name="resource">The resource that was not found</param>
    /// <returns>A new not found error</returns>
    public static Error NotFound(string resource) => new("NOT_FOUND", $"{resource} was not found");

    /// <summary>
    /// Creates an unauthorized error.
    /// </summary>
    /// <param name="message">The unauthorized error message</param>
    /// <returns>A new unauthorized error</returns>
    public static Error Unauthorized(string message = "Unauthorized access") =>
        new("UNAUTHORIZED", message);

    /// <summary>
    /// Creates a forbidden error.
    /// </summary>
    /// <param name="message">The forbidden error message</param>
    /// <returns>A new forbidden error</returns>
    public static Error Forbidden(string message = "Access forbidden") => new("FORBIDDEN", message);

    /// <summary>
    /// Creates a conflict error.
    /// </summary>
    /// <param name="message">The conflict error message</param>
    /// <returns>A new conflict error</returns>
    public static Error Conflict(string message) => new("CONFLICT", message);

    /// <summary>
    /// Creates an internal error.
    /// </summary>
    /// <param name="message">The internal error message</param>
    /// <returns>A new internal error</returns>
    public static Error Internal(string message = "An internal error occurred") =>
        new("INTERNAL_ERROR", message);

    /// <summary>
    /// Creates a file system error.
    /// </summary>
    /// <param name="message">The file system error message</param>
    /// <returns>A new file system error</returns>
    public static Error FileSystem(string message) => new("FILE_SYSTEM_ERROR", message);

    /// <summary>
    /// Creates a parsing error.
    /// </summary>
    /// <param name="message">The parsing error message</param>
    /// <returns>A new parsing error</returns>
    public static Error Parsing(string message) => new("PARSING_ERROR", message);

    /// <summary>
    /// Creates a network error.
    /// </summary>
    /// <param name="message">The network error message</param>
    /// <returns>A new network error</returns>
    public static Error Network(string message) => new("NETWORK_ERROR", message);

    /// <summary>
    /// Creates a timeout error.
    /// </summary>
    /// <param name="message">The timeout error message</param>
    /// <returns>A new timeout error</returns>
    public static Error Timeout(string message = "Operation timed out") => new("TIMEOUT", message);

    public bool Equals(Error? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return Code.Equals(other.Code, StringComparison.OrdinalIgnoreCase)
            && Message.Equals(other.Message, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Error);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Code.ToUpperInvariant(), Message);
    }

    public override string ToString()
    {
        return $"[{Code}] {Message}";
    }

    public static bool operator ==(Error? left, Error? right)
    {
        return EqualityComparer<Error>.Default.Equals(left, right);
    }

    public static bool operator !=(Error? left, Error? right)
    {
        return !(left == right);
    }
}
