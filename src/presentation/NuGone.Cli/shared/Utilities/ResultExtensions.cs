using NuGone.Cli.Shared.Models;

namespace NuGone.Cli.Shared.Utilities;

/// <summary>
/// Extension methods for working with Result types.
/// </summary>
internal static class ResultExtensions
{
    /// <summary>
    /// Converts a Result to a Result&lt;T&gt; with a specific value on success.
    /// </summary>
    public static Result<T> ToResult<T>(this Result result, T value)
    {
        return result.IsSuccess ? Result<T>.Success(value) : Result<T>.Failure(result.Error);
    }

    /// <summary>
    /// Executes an action if the result is successful.
    /// </summary>
    public static Result<T> OnSuccess<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess)
        {
            action(result.Value);
        }
        return result;
    }

    /// <summary>
    /// Executes an action if the result is a failure.
    /// </summary>
    public static Result<T> OnFailure<T>(this Result<T> result, Action<Error> action)
    {
        if (result.IsFailure)
        {
            action(result.Error);
        }
        return result;
    }

    /// <summary>
    /// Combines multiple results into a single result.
    /// Returns the first failure encountered, or success if all succeed.
    /// </summary>
    public static Result Combine(params Result[] results)
    {
        foreach (var result in results)
        {
            if (result.IsFailure)
                return result;
        }
        return Result.Success();
    }

    /// <summary>
    /// Safely executes a function that might throw exceptions and converts them to Result.
    /// </summary>
    public static Result<T> Try<T>(Func<T> func)
    {
        try
        {
            return Result<T>.Success(func());
        }
        catch (ArgumentException ex)
        {
            return Error.InvalidArgument(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Error.AccessDenied(ex.Message);
        }
        catch (DirectoryNotFoundException ex)
        {
            return Error.DirectoryNotFound(ex.Message);
        }
        catch (FileNotFoundException ex)
        {
            return Error.FileNotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return Error.OperationFailed("operation", ex.Message);
        }
    }

    /// <summary>
    /// Safely executes an action that might throw exceptions and converts them to Result.
    /// </summary>
    public static Result Try(Action action)
    {
        try
        {
            action();
            return Result.Success();
        }
        catch (ArgumentException ex)
        {
            return Error.InvalidArgument(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Error.AccessDenied(ex.Message);
        }
        catch (DirectoryNotFoundException ex)
        {
            return Error.DirectoryNotFound(ex.Message);
        }
        catch (FileNotFoundException ex)
        {
            return Error.FileNotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return Error.OperationFailed("operation", ex.Message);
        }
    }
}
