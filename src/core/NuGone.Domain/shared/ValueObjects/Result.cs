namespace NuGone.Domain.Shared.ValueObjects;

/// <summary>
/// Represents the result of an operation that can either succeed or fail.
/// Implements the Result pattern for error handling without exceptions.
/// </summary>
/// <typeparam name="T">The type of the success value</typeparam>
public class Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    private Result(T value)
    {
        _value = value;
        _error = null;
        IsSuccess = true;
    }

    private Result(Error error)
    {
        _value = default;
        _error = error ?? throw new ArgumentNullException(nameof(error));
        IsSuccess = false;
    }

    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Indicates whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the success value. Throws if the result is a failure.
    /// </summary>
    public T Value
    {
        get
        {
            if (IsFailure)
                throw new InvalidOperationException(
                    $"Cannot access value of a failed result. Error: {_error}"
                );

            return _value!;
        }
    }

    /// <summary>
    /// Gets the error. Throws if the result is a success.
    /// </summary>
    public Error Error
    {
        get
        {
            if (IsSuccess)
                throw new InvalidOperationException("Cannot access error of a successful result.");

            return _error!;
        }
    }

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    /// <param name="value">The success value</param>
    /// <returns>A successful result</returns>
    public static Result<T> Success(T value) => new(value);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    /// <param name="error">The error</param>
    /// <returns>A failed result</returns>
    public static Result<T> Failure(Error error) => new(error);

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <returns>A failed result</returns>
    public static Result<T> Failure(string message) => new(Error.Create(message));

    /// <summary>
    /// Creates a failed result with the specified error code and message.
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="message">The error message</param>
    /// <returns>A failed result</returns>
    public static Result<T> Failure(string code, string message) =>
        new(Error.Create(code, message));

    /// <summary>
    /// Executes the specified action if the result is successful.
    /// </summary>
    /// <param name="action">The action to execute</param>
    /// <returns>The current result</returns>
    public Result<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess)
            action(_value!);

        return this;
    }

    /// <summary>
    /// Executes the specified action if the result is a failure.
    /// </summary>
    /// <param name="action">The action to execute</param>
    /// <returns>The current result</returns>
    public Result<T> OnFailure(Action<Error> action)
    {
        if (IsFailure)
            action(_error!);

        return this;
    }

    /// <summary>
    /// Transforms the result value if successful.
    /// </summary>
    /// <typeparam name="TNew">The type of the new value</typeparam>
    /// <param name="func">The transformation function</param>
    /// <returns>A new result with the transformed value or the original error</returns>
    public Result<TNew> Map<TNew>(Func<T, TNew> func)
    {
        if (IsFailure)
            return Result<TNew>.Failure(_error!);

        return Result<TNew>.Success(func(_value!));
    }

    /// <summary>
    /// Binds the result to another operation if successful.
    /// </summary>
    /// <typeparam name="TNew">The type of the new result</typeparam>
    /// <param name="func">The binding function</param>
    /// <returns>The result of the binding function or the original error</returns>
    public Result<TNew> Bind<TNew>(Func<T, Result<TNew>> func)
    {
        if (IsFailure)
            return Result<TNew>.Failure(_error!);

        return func(_value!);
    }

    /// <summary>
    /// Gets the value if successful, otherwise returns the default value.
    /// </summary>
    /// <param name="defaultValue">The default value to return on failure</param>
    /// <returns>The value or default value</returns>
    public T GetValueOrDefault(T defaultValue = default!)
    {
        return IsSuccess ? _value! : defaultValue;
    }

    /// <summary>
    /// Implicit conversion from value to successful result.
    /// </summary>
    /// <param name="value">The value</param>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>
    /// Implicit conversion from error to failed result.
    /// </summary>
    /// <param name="error">The error</param>
    public static implicit operator Result<T>(Error error) => Failure(error);

    public override string ToString()
    {
        return IsSuccess ? $"Success: {_value}" : $"Failure: {_error}";
    }
}

/// <summary>
/// Represents the result of an operation that can either succeed or fail without a return value.
/// </summary>
public class Result
{
    private readonly Error? _error;

    private Result()
    {
        _error = null;
        IsSuccess = true;
    }

    private Result(Error error)
    {
        _error = error ?? throw new ArgumentNullException(nameof(error));
        IsSuccess = false;
    }

    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Indicates whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error. Throws if the result is a success.
    /// </summary>
    public Error Error
    {
        get
        {
            if (IsSuccess)
                throw new InvalidOperationException("Cannot access error of a successful result.");

            return _error!;
        }
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result</returns>
    public static Result Success() => new();

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    /// <param name="error">The error</param>
    /// <returns>A failed result</returns>
    public static Result Failure(Error error) => new(error);

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <returns>A failed result</returns>
    public static Result Failure(string message) => new(Error.Create(message));

    /// <summary>
    /// Creates a failed result with the specified error code and message.
    /// </summary>
    /// <param name="code">The error code</param>
    /// <param name="message">The error message</param>
    /// <returns>A failed result</returns>
    public static Result Failure(string code, string message) => new(Error.Create(code, message));

    /// <summary>
    /// Implicit conversion from error to failed result.
    /// </summary>
    /// <param name="error">The error</param>
    public static implicit operator Result(Error error) => Failure(error);

    public override string ToString()
    {
        return IsSuccess ? "Success" : $"Failure: {_error}";
    }
}
