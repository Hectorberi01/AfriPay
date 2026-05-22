namespace AfriPay.Domain.Currency;

/// <summary>
/// Paire de devises immuable (ex : EUR/XOF).
/// Comparable par valeur — deux CurrencyPair avec les mêmes codes sont égaux.
/// </summary>
public sealed record CurrencyPair
{
    /// <summary>Devise source (ISO 4217, 3 caractères).</summary>
    public string From { get; }

    /// <summary>Devise cible (ISO 4217, 3 caractères).</summary>
    public string To   { get; }

    public CurrencyPair(string from, string to)
    {
        if (string.IsNullOrWhiteSpace(from) || from.Length != 3)
            throw new CurrencyDomainException($"Invalid source currency code: '{from}'. Must be 3-char ISO 4217.");

        if (string.IsNullOrWhiteSpace(to) || to.Length != 3)
            throw new CurrencyDomainException($"Invalid target currency code: '{to}'. Must be 3-char ISO 4217.");

        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
            throw new CurrencyDomainException("Source and target currencies must be different.");

        From = from.ToUpperInvariant();
        To   = to.ToUpperInvariant();
    }

    // Paires prédéfinies

    /// <summary>Euro → Franc CFA UEMOA (taux fixe 655.957).</summary>
    public static readonly CurrencyPair EurXof = new("EUR", "XOF");

    /// <summary>Dollar US → Franc CFA UEMOA.</summary>
    public static readonly CurrencyPair UsdXof = new("USD", "XOF");

    /// <summary>Euro → Cedi ghanéen.</summary>
    public static readonly CurrencyPair EurGhs = new("EUR", "GHS");

    /// <summary>Franc CFA → Euro (conversion inverse pour les virements marchands).</summary>
    public static readonly CurrencyPair XofEur = new("XOF", "EUR");

    /// <summary>Franc CFA UEMOA → Franc CFA CEMAC (même valeur, zones différentes).</summary>
    public static readonly CurrencyPair XofXaf = new("XOF", "XAF");

    public override string ToString() => $"{From}/{To}";

    /// <summary>Retourne la paire inverse (ex : EUR/XOF → XOF/EUR).</summary>
    public CurrencyPair Reverse() => new(To, From);

    /// <summary>Vérifie si une devise appartient à cette paire.</summary>
    public bool Contains(string currency) =>
        From.Equals(currency, StringComparison.OrdinalIgnoreCase) ||
        To.Equals(currency,   StringComparison.OrdinalIgnoreCase);
}

/// <summary>Exception levée pour toute violation des règles métier de la couche Currency.</summary>
public sealed class CurrencyDomainException(string message)
    : Exception(message);