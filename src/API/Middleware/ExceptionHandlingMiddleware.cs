using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AfriPay.API.Middleware;

/// <summary>
/// Middleware de gestion globale des exceptions.
///
/// Doit être le PREMIER middleware dans le pipeline (position 0)
/// pour capturer toutes les exceptions levées en aval.
///
/// Mapping exception → HTTP :
///   - ValidationException (FluentValidation) → 422 Unprocessable Entity
///   - OperationCanceledException             → 499 Client Closed Request (nginx convention)
///   - Exception non gérée                   → 500 Internal Server Error
///
/// Ne log jamais les données sensibles (clés API, tokens, montants exacts).
/// </summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware>  logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (OperationCanceledException) when (ctx.RequestAborted.IsCancellationRequested)
        {
            // Le client a fermé la connexion — pas une erreur serveur
            logger.LogDebug(
                "Request cancelled by client: {Method} {Path}",
                ctx.Request.Method, ctx.Request.Path);

            ctx.Response.StatusCode = 499;  // nginx convention : Client Closed Request
        }
        catch (ValidationException ex)
        {
            logger.LogInformation(
                "Validation failed for {Method} {Path}: {Errors}",
                ctx.Request.Method,
                ctx.Request.Path,
                string.Join(" | ", ex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));

            ctx.Response.StatusCode  = 422;
            ctx.Response.ContentType = "application/json";

            await ctx.Response.WriteAsJsonAsync(new
            {
                code    = "VALIDATION_ERROR",
                message = "One or more validation errors occurred.",
                status  = 422,
                errors  = ex.Errors.Select(e => new
                {
                    field   = ToCamelCase(e.PropertyName),
                    message = e.ErrorMessage,
                }).ToList(),
            });
        }
        catch (Exception ex)
        {
            // Génère un ID de corrélation pour faciliter le débogage
            var correlationId = Guid.NewGuid().ToString("N")[..12];

            logger.LogError(
                ex,
                "Unhandled exception [{CorrelationId}] on {Method} {Path}",
                correlationId,
                ctx.Request.Method,
                ctx.Request.Path);

            if (ctx.Response.HasStarted)
            {
                logger.LogWarning(
                    "Response already started — cannot write error for [{CorrelationId}].",
                    correlationId);
                return;
            }

            ctx.Response.StatusCode  = 500;
            ctx.Response.ContentType = "application/json";

            await ctx.Response.WriteAsJsonAsync(new
            {
                code           = "INTERNAL_ERROR",
                message        = "An unexpected error occurred. Please try again or contact support.",
                status         = 500,
                correlation_id = correlationId,
                doc_url        = "https://docs.afripay.io/errors#internal-error",
            });
        }
    }

    private static string ToCamelCase(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToLowerInvariant(s[0]) + s[1..];
    }
}