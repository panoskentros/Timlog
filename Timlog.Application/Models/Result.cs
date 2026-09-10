namespace Timlog.Application.Models;

public interface IResult
{
    bool Success { get; }
    string? Message { get; }
}

public interface IResult<T> : IResult
{
    T Data { get; }
}

public sealed class Result : IResult
{
    public bool Success { get; init; }
    public string? Message { get; init; }

    public static Result Ok(string? message = null) =>
        new() { Success = true, Message = message };

    public static Result Fail(string message) =>
        new() { Success = false, Message = message };
}

public sealed class Result<T> : IResult<T>
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public T? Data { get; init; }

    public static Result<T> Ok(T data, string? message = null) =>
        new() { Success = true, Message = message, Data = data };

    public static Result<T> Fail(string message) =>
        new() { Success = false, Message = message, Data = default };
}