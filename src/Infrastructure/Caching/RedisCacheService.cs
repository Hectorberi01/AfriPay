using System.Text.Json;
using AfriPay.Domain.Currency;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace AfriPay.Infrastructure.Caching;

/// <summary>
/// Implémentation de ICacheService.
///
/// Utilise IDistributedCache (Redis en production, mémoire en dev/test)
/// pour toutes les données partagées entre instances.
///
/// IMemoryCache est utilisé uniquement pour les tokens OAuth2
/// (in-process, durée courte, pas besoin de partage inter-instances).
/// </summary>
public sealed class RedisCacheService(
    IDistributedCache        distributed,
    IMemoryCache             memory,
    ILogger<RedisCacheService> logger) : ICacheService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };
 
    // GÉNÉRIQUE
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        where T : class
    {
        try
        {
            var bytes = await distributed.GetAsync(key, ct);
            if (bytes is null or { Length: 0 })
                return null;
 
            return JsonSerializer.Deserialize<T>(bytes, JsonOpts);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache GET failed for key '{Key}'. Falling through to source.", key);
            return null;
        }
    }
 
    public async Task SetAsync<T>(
        string            key,
        T                 value,
        TimeSpan          ttl,
        CancellationToken ct = default)
        where T : class
    {
        try
        {
            var bytes   = JsonSerializer.SerializeToUtf8Bytes(value, JsonOpts);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl,
            };
 
            await distributed.SetAsync(key, bytes, options, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache SET failed for key '{Key}'. Continuing without cache.", key);
        }
    }
 
    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await distributed.RemoveAsync(key, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache REMOVE failed for key '{Key}'.", key);
        }
    }
 
    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var bytes = await distributed.GetAsync(key, ct);
            return bytes is not null && bytes.Length > 0;
        }
        catch
        {
            return false;
        }
    }
 
    public async Task<T> GetOrSetAsync<T>(
        string            key,
        Func<Task<T>>     factory,
        TimeSpan          ttl,
        CancellationToken ct = default)
        where T : class
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached is not null)
            return cached;
 
        var value = await factory();
        await SetAsync(key, value, ttl, ct);
        return value;
    }
 
    // IDEMPOTENCE
    public async Task SetIdempotencyAsync(
        Guid              merchantId,
        string            idempotencyKey,
        string            responseJson,
        CancellationToken ct = default)
    {
        var key   = CacheKeys.Idempotency(merchantId, idempotencyKey);
        var bytes = System.Text.Encoding.UTF8.GetBytes(responseJson);
        var opts  = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheTtl.Idempotency,
        };
 
        try
        {
            await distributed.SetAsync(key, bytes, opts, ct);
            logger.LogDebug("Idempotency key cached: merchant={MerchantId} key={Key}", merchantId, idempotencyKey);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to cache idempotency key '{Key}'.", idempotencyKey);
        }
    }
 
    public async Task<string?> GetIdempotencyAsync(
        Guid              merchantId,
        string            idempotencyKey,
        CancellationToken ct = default)
    {
        var key = CacheKeys.Idempotency(merchantId, idempotencyKey);
 
        try
        {
            var bytes = await distributed.GetAsync(key, ct);
            return bytes is null ? null : System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to read idempotency key '{Key}' from cache.", idempotencyKey);
            return null;
        }
    }
 
    // TAUX DE CHANGE
    public async Task SetExchangeRateAsync(
        CurrencyPair      pair,
        ExchangeRate      rate,
        CancellationToken ct = default)
    {
        var key = CacheKeys.ExchangeRate(pair);
 
        // Durée de cache = durée de validité du taux (calculée dans le domaine)
        var remainingValidity = rate.ValidUntil - DateTimeOffset.UtcNow;
        var ttl = remainingValidity > TimeSpan.Zero
            ? remainingValidity
            : CacheTtl.ExchangeRateMarket;
 
        await SetAsync(key, rate, ttl, ct);
 
        logger.LogDebug("Exchange rate cached: {Pair} = {Rate} (valid for {Ttl})", pair, rate.EffectiveRate, ttl);
    }
 
    public async Task<ExchangeRate?> GetExchangeRateAsync(
        CurrencyPair      pair,
        CancellationToken ct = default)
        => await GetAsync<ExchangeRate>(CacheKeys.ExchangeRate(pair), ct);
 
    // CONTEXTE MARCHAND
    public async Task SetMerchantContextAsync(
        string             keyHash,
        MerchantCacheEntry entry,
        CancellationToken  ct = default)
    {
        var key = CacheKeys.MerchantContext(keyHash);
        await SetAsync(key, entry, CacheTtl.MerchantContext, ct);
    }
 
    public async Task<MerchantCacheEntry?> GetMerchantContextAsync(
        string            keyHash,
        CancellationToken ct = default)
        => await GetAsync<MerchantCacheEntry>(
            CacheKeys.MerchantContext(keyHash), ct);
 
    public async Task InvalidateMerchantContextAsync(
        string            keyHash,
        CancellationToken ct = default)
    {
        var key = CacheKeys.MerchantContext(keyHash);
        await RemoveAsync(key, ct);
        logger.LogInformation("Merchant context invalidated for keyHash={KeyHash}.", keyHash);
    }
 
    // TOKEN MTN MOMO (IMemoryCache — in-process uniquement)
    private const string MtnTokenKeyPrefix = "mtn_token:";
 
    /// <summary>
    /// Met en cache le token OAuth2 MTN MoMo dans IMemoryCache.
    /// In-process uniquement — chaque instance gère son propre token.
    /// Durée : 59 minutes (expire en 60 min côté MTN).
    /// </summary>
    public void SetMtnToken(string apiUserId, string token)
    {
        memory.Set($"{MtnTokenKeyPrefix}{apiUserId}", token,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheTtl.MtnOAuthToken,
                Size = 1,
            });
    }
 
    /// <summary>Récupère le token MTN MoMo depuis le cache in-process.</summary>
    public string? GetMtnToken(string apiUserId)
        => memory.TryGetValue($"{MtnTokenKeyPrefix}{apiUserId}", out string? token)
            ? token
            : null;
}