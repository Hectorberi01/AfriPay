namespace AfriPay.Application.Common.Errors;

//Result pattern
// Évite les exceptions pour le flux métier normal.
// Les exceptions restent pour les cas vraiment exceptionnels (BDD down, etc.)
public sealed class Result<T>
{
    public T?         Value    { get; private init; }
    public AppError?  Error    { get; private init; }
    public bool       IsSuccess => Error is null;
 
    public static Result<T> Ok(T value)          => new() { Value = value };
    public static Result<T> Fail(AppError error)  => new() { Error = error };
 
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<AppError, TOut> onError)
        => IsSuccess ? onSuccess(Value!) : onError(Error!);
}