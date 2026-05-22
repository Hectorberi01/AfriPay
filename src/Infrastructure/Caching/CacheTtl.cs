namespace AfriPay.Infrastructure.Caching;

public static class CacheTtl
{
    /// <summary>Contexte marchand : 5 minutes (invalidé sur changement de plan).</summary>
    public static readonly TimeSpan MerchantContext = TimeSpan.FromMinutes(5);
 
    /// <summary>Taux de change EUR/XOF fixe : 24h (taux fixe BCEAO).</summary>
    public static readonly TimeSpan ExchangeRateFixed = TimeSpan.FromHours(24);
 
    /// <summary>Taux de change marché : 1h (rafraîchi par le CurrencyRefreshJob).</summary>
    public static readonly TimeSpan ExchangeRateMarket = TimeSpan.FromHours(1);
 
    /// <summary>Idempotency key : 24h (fenêtre de déduplication).</summary>
    public static readonly TimeSpan Idempotency = TimeSpan.FromHours(24);
 
    /// <summary>Token OAuth2 MTN MoMo : 59 minutes (expire en 60 min côté MTN).</summary>
    public static readonly TimeSpan MtnOAuthToken = TimeSpan.FromMinutes(59);
}