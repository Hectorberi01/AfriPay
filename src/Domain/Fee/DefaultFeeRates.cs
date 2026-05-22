namespace AfriPay.Domain.Fee;

public static class DefaultFeeRates
{
    // Mobile Money Afrique de l'Ouest
    public const decimal MtnMomo   = 0.8m;
    public const decimal Wave      = 0.8m;
    public const decimal Moov      = 0.9m;
    public const decimal Orange    = 0.9m;
 
    // Carte internationale
    public const decimal Stripe    = 1.2m;
 
    // Wallet
    public const decimal PayPal    = 1.5m;   // 1.5% + coûts PayPal
 
    public static decimal ForProvider(string providerKey)
        => providerKey.ToLowerInvariant() switch
        {
            "mtn_momo"    => MtnMomo,
            "wave"        => Wave,
            "moov_money"  => Moov,
            "orange_money"=> Orange,
            "stripe"      => Stripe,
            "paypal"      => PayPal,
            _             => 1.0m,
        };
}