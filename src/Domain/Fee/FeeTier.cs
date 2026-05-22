namespace AfriPay.Domain.Fee;

public enum FeeTier
{
    MobileMoney,   // MTN MoMo, Wave, Moov, Orange
    Card,          // Stripe
    Wallet,        // PayPal
    Crypto,        // Futur
}