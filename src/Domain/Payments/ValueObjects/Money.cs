using AfriPay.Domain.Exceptions;

namespace AfriPay.Domain.Payments.ValueObjects;

/// <summary>
/// Représente un montant monétaire en unités minimales.
/// XOF : pas de décimale (5000 = 5000 XOF).
/// EUR : centimes (1200 = 12,00 EUR).
/// Immuable — toute opération retourne une nouvelle instance.
/// </summary>
public sealed class Money
{
    public long   Amount   { get; }
    public string Currency { get; }   // ISO 4217
 
    public Money(long amount, string currency)
    {
        if (amount < 0)
            throw new DomainException("Money amount cannot be negative.");
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new DomainException("Currency must be a 3-character ISO 4217 code.");
 
        Amount   = amount;
        Currency = currency.ToUpperInvariant();
    }
    
    /// <summary>Calcule les frais AfriPay selon le type de provider.</summary>
    public static Money CalculateFee(Money amount, string providerKey)
    {
        // 1.2% carte (Stripe, PayPal), 0.8% Mobile Money
        var rate = providerKey is "stripe" or "paypal" ? 0.012m : 0.008m;
 
        // Minimum 50 XOF (ou équivalent centimes)
        var minimum   = amount.Currency == "XOF" ? 50L : 1L;
        var feeAmount = Math.Max(minimum, (long)Math.Ceiling(amount.Amount * rate));
 
        return new Money(feeAmount, amount.Currency);
    }
 
    public Money Subtract(Money other)
    {
        if (other.Currency != Currency)
            throw new DomainException($"Cannot subtract {other.Currency} from {Currency}.");
        return new Money(Amount - other.Amount, Currency);
    }
 
    public static Money Zero(string currency) => new(0, currency);
 
    public override string ToString() => Currency switch
    {
        "XOF" or "GHS" or "XAF" => $"{Amount:N0} {Currency}",
        _                        => $"{Amount / 100.0m:N2} {Currency}",
    };
}