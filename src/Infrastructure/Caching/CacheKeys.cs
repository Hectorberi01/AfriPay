using AfriPay.Domain.Currency;

namespace AfriPay.Infrastructure.Caching;

/// <summary>
/// Centralise toutes les clés de cache.
/// Convention : "{domaine}:{discriminant}" — tout en minuscules.
/// </summary>
public static class CacheKeys
{
    public static string Idempotency(Guid merchantId, string key)
        => $"idempotency:{merchantId}:{key}";
 
    public static string ExchangeRate(string from, string to)
        => $"exchange_rate:{from.ToLowerInvariant()}:{to.ToLowerInvariant()}";
 
    public static string ExchangeRate(CurrencyPair pair)
        => ExchangeRate(pair.From, pair.To);
 
    public static string MerchantContext(string keyHash)
        => $"merchant_ctx:{keyHash}";
}