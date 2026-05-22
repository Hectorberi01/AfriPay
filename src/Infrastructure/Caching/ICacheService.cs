using AfriPay.Domain.Currency;

namespace AfriPay.Infrastructure.Caching;


/// <summary>
/// Service de cache unifié pour AfriPay.
/// Abstrait IDistributedCache (Redis / mémoire) et IMemoryCache (in-process).
///
/// Usages dans la plateforme :
///   - Tokens OAuth2 MTN MoMo             → IMemoryCache  (in-process, courte durée)
///   - Taux de change EUR/XOF             → IDistributedCache (partagé entre instances)
///   - Idempotency keys                   → IDistributedCache (24h, partagé)
///   - Contexte marchand (plan, webhook)  → IDistributedCache (5 min, invalidé sur update)
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;
 
    Task SetAsync<T>(
        string            key,
        T                 value,
        TimeSpan          ttl,
        CancellationToken ct = default)
        where T : class;
 
    Task RemoveAsync(string key, CancellationToken ct = default);
 
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
    
    /// <summary>
    /// Lit depuis le cache ou exécute la factory et met le résultat en cache.
    /// Pattern "get-or-set" — évite les stampedes sur les clés populaires.
    /// </summary>
    Task<T> GetOrSetAsync<T>(
        string            key,
        Func<Task<T>>     factory,
        TimeSpan          ttl,
        CancellationToken ct = default)
        where T : class;
 
    // ── Spécialisé : Idempotence ───────────────────────────────
 
    /// <summary>
    /// Enregistre une réponse idempotente (24h).
    /// Clé : "idempotency:{merchantId}:{idempotencyKey}"
    /// </summary>
    Task SetIdempotencyAsync(
        Guid              merchantId,
        string            idempotencyKey,
        string            responseJson,
        CancellationToken ct = default);
 
    Task<string?> GetIdempotencyAsync(
        Guid              merchantId,
        string            idempotencyKey,
        CancellationToken ct = default);
    
    /// <summary>
    /// Met en cache le taux de change avec la durée de validité du taux.
    /// Clé : "exchange_rate:{from}:{to}"
    /// </summary>
    Task SetExchangeRateAsync(
        CurrencyPair      pair,
        ExchangeRate      rate,
        CancellationToken ct = default);
 
    Task<ExchangeRate?> GetExchangeRateAsync(
        CurrencyPair      pair,
        CancellationToken ct = default);
    
    /// <summary>
    /// Met en cache le contexte marchand résolu depuis sa clé API (5 min).
    /// Clé : "merchant_ctx:{keyHash}"
    /// </summary>
    Task SetMerchantContextAsync(
        string            keyHash,
        MerchantCacheEntry entry,
        CancellationToken  ct = default);
 
    Task<MerchantCacheEntry?> GetMerchantContextAsync(
        string            keyHash,
        CancellationToken ct = default);
 
    Task InvalidateMerchantContextAsync(
        string            keyHash,
        CancellationToken ct = default);

}

/// <summary>
/// Contexte marchand mis en cache pour éviter une lecture DB à chaque requête.
/// Contient uniquement ce dont le middleware d'auth a besoin.
/// </summary>
public sealed record MerchantCacheEntry(
    Guid    MerchantId,
    string  Plan,           // "starter" | "growth" | "scale"
    int     RatePerMinute,
    bool    CanAccessAllProviders,
    string? WebhookUrl,
    string? WebhookSecret,
    bool   IsLive
);