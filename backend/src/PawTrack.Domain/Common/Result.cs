namespace PawTrack.Domain.Common;

public sealed class Result<T>
{
    private Result(T? value, bool isSuccess, IReadOnlyList<string> errors)
    {
        Value = value;
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public T? Value { get; }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<string> Errors { get; }

    public static Result<T> Success(T value) =>
        new(value, true, []);

    public static Result<T> Failure(IEnumerable<string> errors) =>
        new(default, false, errors.ToList().AsReadOnly());

    public static Result<T> Failure(string error) =>
        new(default, false, [error]);
}

public static class Result
{
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(string error) => Result<T>.Failure(error);
    public static Result<T> Failure<T>(IEnumerable<string> errors) => Result<T>.Failure(errors);
}
