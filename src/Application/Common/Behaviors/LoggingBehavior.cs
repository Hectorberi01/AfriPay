using System.Diagnostics;
using MediatR;

namespace AfriPay.Application.Common.Behaviors;

/// <summary>
/// Pipeline MediatR — journalise chaque commande et query avec :
///   - Nom du handler
///   - Durée d'exécution (ms)
///   - Résultat : Success | Failure (code d'erreur)
///   - Niveau WARNING si durée > 500ms (slow query detection)
///
/// Position dans le pipeline : après ValidationBehavior, avant le handler.
/// Ainsi on ne loggue que les requêtes qui ont passé la validation.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    // Seuil au-delà duquel on émet un WARNING "slow handler"
    private const int SlowHandlerThresholdMs = 500;

    public async Task<TResponse> Handle(
        TRequest                          request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken                 ct)
    {
        var requestName = typeof(TRequest).Name;
        var sw          = Stopwatch.StartNew();

        logger.LogInformation("→ Handling {RequestName}", requestName);

        try
        {
            var response = await next();

            sw.Stop();

            // Détecte si le résultat est un Result<T> en échec
            var isFailure = IsResultFailure(response);

            if (isFailure)
            {
                var errorCode = ExtractErrorCode(response);
                logger.LogWarning(
                    "← {RequestName} failed in {ElapsedMs}ms — error: {ErrorCode}",
                    requestName, sw.ElapsedMilliseconds, errorCode);
            }
            else if (sw.ElapsedMilliseconds > SlowHandlerThresholdMs)
            {
                logger.LogWarning("← {RequestName} completed in {ElapsedMs}ms [SLOW]", requestName, sw.ElapsedMilliseconds);
            }
            else
            {
                logger.LogInformation(
                    "← {RequestName} completed in {ElapsedMs}ms",
                    requestName, sw.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(
                ex,
                "← {RequestName} threw {ExceptionType} after {ElapsedMs}ms",
                requestName, ex.GetType().Name, sw.ElapsedMilliseconds);

            throw;
        }
    }

    //Helpers

    /// <summary>
    /// Détecte si TResponse est un Result;T&gt; en échec
    /// sans coupler LoggingBehavior à une implémentation concrète de Result.
    /// </summary>
    private static bool IsResultFailure(TResponse? response)
    {
        if (response is null) return false;

        var type = typeof(TResponse);

        // Cherche une propriété "IsSuccess" ou "Error" sur le type de réponse
        var isSuccessProp = type.GetProperty("IsSuccess");
        if (isSuccessProp?.GetValue(response) is bool isSuccess)
            return !isSuccess;

        return false;
    }

    private static string? ExtractErrorCode(TResponse? response)
    {
        if (response is null) return null;

        // Cherche Error.Code via réflexion pour ne pas coupler sur AppError
        var errorProp = typeof(TResponse).GetProperty("Error");
        var error     = errorProp?.GetValue(response);
        if (error is null) return null;

        var codeProp = error.GetType().GetProperty("Code");
        return codeProp?.GetValue(error)?.ToString();
    }
}