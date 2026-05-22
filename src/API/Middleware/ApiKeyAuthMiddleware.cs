using System.Security.Cryptography;
using System.Text;
using AfriPay.Domain.Merchants;
using AfriPay.Domain.Repositories;
using AfriPay.Infrastructure.Caching;

namespace AfriPay.API.Middleware;

/// <summary>
/// Middleware d'authentification par clé API.
///
/// Flux :
///   1. Lit le header Authorization: Bearer afp_live_sk_...
///   2. Calcule le SHA-256 de la clé
///   3. Vérifie dans le cache Redis (MerchantCacheEntry) → hit = fast path
///   4. Si cache miss → lit en base via IUnitOfWork
///   5. Stocke le contexte marchand dans HttpContext.Items
///
/// Routes exemptées (pas d'authentification requise) :
///   - GET  /health
///   - POST /webhooks/providers/*   (callbacks entrants des providers)
/// </summary>
public sealed class ApiKeyAuthMiddleware(RequestDelegate next, ILogger<ApiKeyAuthMiddleware> logger)
{
    private const string ApiKeyHeader = "X-API-Key";
    
    private static readonly HashSet<string> ExemptPrefixes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "/health",
            "/webhooks/providers",
            "/docs",
            "/swagger",
            "/swagger-ui",
            "/favicon.ico",
            "/v1/auth",
            "/v1/team",
            "/v1/merchants"
        };

    // Routes exemptées uniquement pour une méthode HTTP précise
    // (POST /v1/merchants = création de compte sans clé API)
    private static readonly Dictionary<string, HashSet<string>> ExemptByMethod =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["POST"] = new(StringComparer.OrdinalIgnoreCase)
            {
                "/v1/merchants",
                "/v1/auth/register",
                "/v1/auth/login",
            },
        };

    public async Task InvokeAsync(HttpContext  ctx, IUnitOfWork  uow, ICacheService cache)
    {
        // Routes exemptées
        if (IsExempt(ctx.Request.Path, ctx.Request.Method))
        {
            await next(ctx);
            return;
        }

        // Lecture et validation du header
        if (!ctx.Request.Headers.TryGetValue(ApiKeyHeader, out var apiKeyHeader))
        {
            await WriteUnauthorized(ctx, "MISSING_API_KEY",
                "X-API-Key header is required.");
            return;
        }
        
        var rawKey = apiKeyHeader.ToString().Trim();
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            await WriteUnauthorized(ctx, "EMPTY_API_KEY", "API key is empty.");
            return;
        }

        // Calcul du hash SHA-256 (jamais stocker la clé brute)
        var keyHash = ComputeHash(rawKey);

        logger.LogDebug(
            "Auth attempt: key_prefix={Prefix} hash_prefix={HashPrefix}",
            rawKey[..Math.Min(12, rawKey.Length)],
            keyHash[..8]);

        // cache Redis
        var cachedCtx = await cache.GetMerchantContextAsync(keyHash, ctx.RequestAborted);

        if (cachedCtx is not null)
        {
            SetMerchantContext(ctx, cachedCtx);
            await next(ctx);
            return;
        }

        // DB fallback
        var merchant = await uow.Merchants.GetByApiKeyHashAsync(keyHash, ctx.RequestAborted);

        if (merchant is null)
        {
            logger.LogWarning(
                "Invalid API key attempt. Hash prefix: {HashPrefix}",
                keyHash[..8]);
            await WriteUnauthorized(ctx, "INVALID_API_KEY", "Invalid API key.");
            return;
        }

        if (merchant.Status is MerchantStatus.Suspended or MerchantStatus.Closed)
        {
            logger.LogWarning(
                "Blocked merchant access: merchantId={MerchantId} status={Status}",
                merchant.Id, merchant.Status);
            await WriteUnauthorized(ctx, "MERCHANT_INACTIVE",
                $"Merchant account is {merchant.Status.ToString().ToLower()}. " +
                "Contact support@afripay.io.");
            return;
        }

        // Détecter live/sandbox depuis le préfixe de la clé API
        // afp_live_sk_... → live production
        // afp_test_sk_... → sandbox
        var isLive = rawKey.StartsWith("afp_live_", StringComparison.OrdinalIgnoreCase);
        
        // Construire le contexte marchand
        // Limits peut être null si OwnsOne non chargé — on utilise des valeurs par défaut sûres
        var limits = merchant.Limits;
        var entry  = new MerchantCacheEntry(
            MerchantId:            merchant.Id,
            Plan:                  merchant.Plan.ToString().ToLower(),
            RatePerMinute:         limits?.RatePerMinute         ?? 100,
            CanAccessAllProviders: limits?.CanAccessAllProviders ?? false,
            WebhookUrl:            merchant.WebhookConfig?.Url,
            WebhookSecret:         merchant.WebhookConfig?.Secret,
            IsLive:                isLive
            );

        logger.LogDebug(
            "Auth OK: merchant={MerchantId} plan={Plan} status={Status}",
            merchant.Id, entry.Plan, merchant.Status);

        // Poser le contexte dans HttpContext.Items AVANT le cache et AVANT next()
        SetMerchantContext(ctx, entry);

        // Cache asynchrone (best-effort — ne bloque pas la requête si Redis down)
        _ = cache.SetMerchantContextAsync(keyHash, entry, CancellationToken.None);

        // Mettre à jour le last_used_at (fire-and-forget)
        _ = UpdateLastUsedAsync(uow, keyHash, CancellationToken.None);

        await next(ctx);
    }

    // Helpers

    private static bool IsExempt(PathString path, string method)
    {
        // Exemption globale (toutes méthodes)
        if (ExemptPrefixes.Any(p =>
            path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase)))
            return true;

        // Exemption par méthode HTTP (ex : POST /v1/merchants uniquement)
        if (ExemptByMethod.TryGetValue(method, out var paths))
            return paths.Any(p =>
                path.Value?.Equals(p, StringComparison.OrdinalIgnoreCase) == true);

        return false;
    }

    private static void SetMerchantContext(HttpContext ctx, MerchantCacheEntry entry)
    {
        ctx.Items[HttpContextKeys.MerchantId]    = entry.MerchantId;
        ctx.Items[HttpContextKeys.MerchantPlan]  = entry.Plan;
        ctx.Items[HttpContextKeys.RatePerMinute] = entry.RatePerMinute;
        ctx.Items[HttpContextKeys.WebhookUrl]    = entry.WebhookUrl;
        ctx.Items[HttpContextKeys.WebhookSecret] = entry.WebhookSecret;
        ctx.Items[HttpContextKeys.IsLive]        = entry.IsLive;
    }

    private static string ComputeHash(string key)
    {
        var bytes = Encoding.UTF8.GetBytes(key);
        var hash  = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
    private static async Task WriteUnauthorized(HttpContext ctx, string code, string message)
    {
        ctx.Response.StatusCode  = 401;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsJsonAsync(new
        {
            code,
            message,
            status  = 401,
            doc_url = $"https://docs.afripay.io/errors#{code.ToLowerInvariant().Replace('_', '-')}",
        });
    }

    private static async Task UpdateLastUsedAsync(
        IUnitOfWork uow, string keyHash, CancellationToken ct)
    {
        try
        {
            var merchant = await uow.Merchants.GetByApiKeyHashAsync(keyHash, ct);
            var key      = merchant?.ApiKeys.FirstOrDefault(k => k.KeyHash == keyHash);
            if (key is null) return;

            // LastUsedAt mis à jour via une méthode sur ApiKey si nécessaire
            await uow.SaveChangesAsync(ct);
        }
        catch
        {
            // Fire-and-forget — ne jamais faire planter la requête pour ça
        }
    }
}