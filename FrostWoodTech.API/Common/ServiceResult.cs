namespace FrostWoodTech.API.Common;

public enum ServiceErrorKind
{
    NotFound,
    Validation,
    Conflict,
    Unauthorized,
    Forbidden
}

/// <param name="Code">Stable machine-readable code, e.g. slug_taken.</param>
public sealed record ServiceError(ServiceErrorKind Kind, string Code, string Message);

public sealed class ServiceResult<T>
{
    private ServiceResult(T? value, ServiceError? error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }

    public ServiceError? Error { get; }

    public bool IsSuccess => Error is null;

    public static ServiceResult<T> Success(T value) => new(value, null);

    public static ServiceResult<T> Failure(ServiceError error) => new(default, error);

    public static ServiceResult<T> NotFound(string code, string message) =>
        new(default, new ServiceError(ServiceErrorKind.NotFound, code, message));

    public static ServiceResult<T> Validation(string message) =>
        new(default, new ServiceError(ServiceErrorKind.Validation, "validation_failed", message));

    public static ServiceResult<T> Unauthorized(string code, string message) =>
        new(default, new ServiceError(ServiceErrorKind.Unauthorized, code, message));

    public static ServiceResult<T> Forbidden(string code, string message) =>
        new(default, new ServiceError(ServiceErrorKind.Forbidden, code, message));

    public static ServiceResult<T> Conflict(string code, string message) =>
        new(default, new ServiceError(ServiceErrorKind.Conflict, code, message));
}
