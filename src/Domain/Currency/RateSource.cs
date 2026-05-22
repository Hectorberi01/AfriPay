namespace AfriPay.Domain.Currency;

/// <summary>
/// Source du taux de change enregistré.
/// Utilisé pour la traçabilité et la réconciliation comptable.
/// </summary>
public enum RateSource
{
    /// <summary>
    /// Banque Centrale des États de l'Afrique de l'Ouest.
    /// Source officielle pour EUR/XOF.
    /// </summary>
    Bceao,

    /// <summary>
    /// Banque Centrale Européenne.
    /// Source pour les paires EUR/*.
    /// </summary>
    Ecb,

    /// <summary>
    /// Taux fixe officiel EUR/XOF (655.957 depuis 1999).
    /// Garanti par le Trésor français — aucun appel API nécessaire.
    /// </summary>
    Fixed,

    /// <summary>
    /// API de marché (Fixer.io, Open Exchange Rates, etc.).
    /// Utilisé pour les paires non couvertes par BCEAO/ECB.
    /// </summary>
    MarketApi,
}