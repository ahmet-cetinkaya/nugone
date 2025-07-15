namespace NuGone.Cli.Shared.Models;

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail with an error.
/// </summary>
public readonly struct Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;
    private readonly bool _isSuccess;

    private Result(T value)
    {
        _value = value;
        _error = null;
        _isSuccess = true;
    }

    private Result(Error error)
    {
        _value = default;
        _error = error;
        _isSuccess = false;
    }

    public bool IsSuccess => _isSuccess;
    public bool IsFailure => !_isSuccess;

    public T Value =>
        _isSuccess
            ? _value!
            : throw new InvalidOperationException("Cannot access value of a failed result");
    public Error Error =>
        !_isSuccess
            ? _error!
            : throw new InvalidOperationException("Cannot access error of a successful result");

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure)
    {
        return _isSuccess ? onSuccess(_value!) : onFailure(_error!);
    }

    public Result<TResult> Map<TResult>(Func<T, TResult> mapper)
    {
        return _isSuccess
            ? Result<TResult>.Success(mapper(_value!))
            : Result<TResult>.Failure(_error!);
    }

    public Result<TResult> Bind<TResult>(Func<T, Result<TResult>> binder)
    {
        return _isSuccess ? binder(_value!) : Result<TResult>.Failure(_error!);
    }

    public T GetValueOrDefault(T defaultValue = default!)
    {
        return _isSuccess ? _value! : defaultValue;
    }

    public override string ToString()
    {
        return _isSuccess ? $"Success({_value})" : $"Failure({_error})";
    }
}

/// <summary>
/// Represents the result of an operation that can either succeed or fail with an error (no return value).
/// </summary>
public readonly struct Result
{
    private readonly Error? _error;
    private readonly bool _isSuccess;

    private Result(bool isSuccess, Error? error = null)
    {
        _isSuccess = isSuccess;
        _error = error;
    }

    public bool IsSuccess => _isSuccess;
    public bool IsFailure => !_isSuccess;

    public Error Error =>
        !_isSuccess
            ? _error!
            : throw new InvalidOperationException("Cannot access error of a successful result");

    public static Result Success() => new(true);

    public static Result Failure(Error error) => new(false, error);

    public static implicit operator Result(Error error) => Failure(error);

    public TResult Match<TResult>(Func<TResult> onSuccess, Func<Error, TResult> onFailure)
    {
        return _isSuccess ? onSuccess() : onFailure(_error!);
    }

    public Result<T> Map<T>(Func<T> mapper)
    {
        return _isSuccess ? Result<T>.Success(mapper()) : Result<T>.Failure(_error!);
    }

    public Result Bind(Func<Result> binder)
    {
        return _isSuccess ? binder() : Failure(_error!);
    }

    public override string ToString()
    {
        return _isSuccess ? "Success" : $"Failure({_error})";
    }
}
