namespace AfriPay.Domain.Currency;
 
/// <summary>
/// Service domaine de conversion de devises.
/// Encapsule la logique qui ne peut pas appartenir à un agrégat unique.
/// </summary>
public static class CurrencyConverter
{
    /// <summary>Devises de la zone franc CFA (pas de conversion entre elles).</summary>
    private static readonly HashSet<string> XofZone =
        new(StringComparer.OrdinalIgnoreCase) { "XOF", "XAF" };
 
    /// <summary>
    /// Vérifie si deux devises sont dans la même zone monétaire.
    /// XOF et XAF ont le même taux — aucune conversion nécessaire.
    /// </summary>
    public static bool SameZone(string from, string to) =>
        from.Equals(to, StringComparison.OrdinalIgnoreCase) ||
        (XofZone.Contains(from) && XofZone.Contains(to));
 
    /// <summary>
    /// Convertit un montant entre deux devises avec le taux fourni.
    /// Lève une exception si les devises ne correspondent pas au taux
    /// ou si le taux est expiré.
    /// </summary>
    public static long Convert(
        long         amount,
        string       fromCurrency,
        string       toCurrency,
        ExchangeRate rate)
    {
        if (!rate.Pair.From.Equals(fromCurrency, StringComparison.OrdinalIgnoreCase) ||
            !rate.Pair.To.Equals(toCurrency,     StringComparison.OrdinalIgnoreCase))
            throw new CurrencyDomainException(
                $"Rate pair {rate.Pair} does not match requested " +
                $"conversion {fromCurrency}/{toCurrency}.");
 
        if (rate.IsExpired)
            throw new CurrencyDomainException(
                $"Exchange rate for {rate.Pair} expired at {rate.ValidUntil:u}. " +
                "Fetch a fresh rate before converting.");
 
        return rate.Convert(amount);
    }
}