using AfriPay.Infrastructure.Caching;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AfriPay.API.Middleware;

/// <summary>
/// Middleware d'idempotence.
///
/// Garantit qu'une même requête (même Idempotency-Key + même marchand)
/// retourne toujours le même résultat sans appeler le handler deux fois.
///
/// Flux :
///   1. Vérifie la présence du header Idempotency-Key
///   2. Lookup dans Redis : clé "idempotency:{merchantId}:{key}"
///   3. Cache hit  → retourne la réponse mise en cache (header X-Idempotency-Replayed: true)
///   4. Cache miss → exécute le handler, capture la réponse, met en cache si 2xx
///
/// Endpoints soumis à idempotence (POST uniquement) :
///   - POST /v1/payments/initiate
///   - POST /v1/refunds
///
/// Les GET, DELETE et autres POST ne sont pas concernés.
/// </summary>
public sealed class IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
{
    // Uniquement les endpoints qui créent des ressources
    private static readonly HashSet<string> IdempotentPaths =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "/v1/payments/initiate",
            "/v1/refunds",
        };

    public async Task InvokeAsync(HttpContext ctx, ICacheService cache)
    {
        // Applique uniquement sur les paths et méthodes concernés
        if (!IdempotentPaths.Contains(ctx.Request.Path)
            || !HttpMethods.IsPost(ctx.Request.Method))
        {
            await next(ctx);
            return;
        }

        // Récupère le contexte marchand posé par ApiKeyAuthMiddleware
        if (ctx.Items[HttpContextKeys.MerchantId] is not Guid merchantId)
        {
            // Ne devrait pas arriver — ApiKeyAuthMiddleware est avant dans le pipeline
            await next(ctx);
            return;
        }

        // Valide la présence du header
        if (!ctx.Request.Headers.TryGetValue("Idempotency-Key", out var keyHeader)
            || string.IsNullOrWhiteSpace(keyHeader))
        {
            ctx.Response.StatusCode  = 400;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new
            {
                code    = "MISSING_IDEMPOTENCY_KEY",
                message = "Header 'Idempotency-Key' is required for this endpoint.",
                status  = 400,
            });
            return;
        }

        var idempotencyKey = keyHeader.ToString().Trim();

        // Valide le format (doit être un UUID)
        if (!Guid.TryParse(idempotencyKey, out _))
        {
            ctx.Response.StatusCode  = 400;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new
            {
                code    = "INVALID_IDEMPOTENCY_KEY",
                message = "Header 'Idempotency-Key' must be a valid UUID v4.",
                status  = 400,
            });
            return;
        }

        // Lookup cache
        var cached = await cache.GetIdempotencyAsync(merchantId, idempotencyKey,
            ctx.RequestAborted);

        if (cached is not null)
        {
            logger.LogDebug(
                "Idempotency cache hit: merchant={MerchantId} key={Key}",
                merchantId, idempotencyKey);

            ctx.Response.StatusCode  = 200;
            ctx.Response.ContentType = "application/json";
            ctx.Response.Headers.Append("X-Idempotency-Replayed", "true");
            await ctx.Response.WriteAsync(cached, ctx.RequestAborted);
            return;
        }

        // Cache miss — intercepte la réponse pour la sauvegarder
        var originalBody = ctx.Response.Body;
        await using var buffer = new System.IO.MemoryStream();
        ctx.Response.Body = buffer;

        try
        {
            await next(ctx);

            buffer.Seek(0, System.IO.SeekOrigin.Begin);
            var responseBody = await new StreamReader(buffer).ReadToEndAsync(
                ctx.RequestAborted);

            // Cache uniquement les réponses 2xx
            if (ctx.Response.StatusCode is >= 200 and < 300)
            {
                await cache.SetIdempotencyAsync(
                    merchantId, idempotencyKey, responseBody, ctx.RequestAborted);

                logger.LogDebug(
                    "Idempotency response cached: merchant={MerchantId} key={Key} status={Status}",
                    merchantId, idempotencyKey, ctx.Response.StatusCode);
            }

            // Rejoue la réponse vers le client
            buffer.Seek(0, System.IO.SeekOrigin.Begin);
            await buffer.CopyToAsync(originalBody, ctx.RequestAborted);
        }
        finally
        {
            ctx.Response.Body = originalBody;
        }
    }
}