namespace AfriPay.Domain.Currency;

/// <summary>
/// Agrégat ExchangeRate — historique immuable des taux de change.
///
/// Invariants métier :
///   - Un taux enregistré est IMMUABLE. On n'update jamais un enregistrement
///     existant — on en crée un nouveau. Cela garantit la réconciliation
///     comptable : chaque transaction peut retrouver le taux appliqué.
///   - Le taux effectif = taux officiel × (1 - spread).
///   - Le spread AfriPay est plafonné à 2% et planché à 0%.
///   - Le taux EUR/XOF fixe (655.957) est codé en dur comme constante.
/// </summary>
public sealed class ExchangeRate
{
 
    /// <summary>
    /// Taux EUR/XOF fixe officiel depuis la création du franc CFA (1999).
    /// 1 EUR = 655.957 XOF — garanti par le Trésor français et la BCEAO.
    /// </summary>
    public const decimal EurXofFixedRate = 655.957m;
 
    // Identité 
 
    public Guid         Id           { get; private set; }
    public CurrencyPair Pair         { get; private set; } = default!;
 
    // Taux 
 
    /// <summary>Taux officiel brut (source BCEAO/ECB/marché).</summary>
    public decimal     OfficialRate  { get; private set; }
 
    /// <summary>
    /// Marge AfriPay appliquée sur le taux officiel.
    /// Exprimée en décimal : 0.005 = 0.5%
    /// </summary>
    public decimal     Spread        { get; private set; }
 
    /// <summary>
    /// Taux effectif appliqué aux conversions marchands.
    /// EffectiveRate = OfficialRate × (1 - Spread)
    /// </summary>
    public decimal     EffectiveRate { get; private set; }
 
    public RateSource  Source        { get; private set; }
 
    //Timestamps
 
    public DateTimeOffset RecordedAt  { get; private set; }
 
    /// <summary>
    /// Indique jusqu'à quand ce taux est considéré valide.
    /// Après cette date, le service doit en obtenir un nouveau.
    /// </summary>
    public DateTimeOffset ValidUntil  { get; private set; }
 
    // Computed
 
    public bool IsExpired => DateTimeOffset.UtcNow > ValidUntil;
 
    private ExchangeRate() { }
 
    /// <summary>
    /// Enregistre un nouveau taux de change.
    /// Calcule automatiquement le taux effectif après spread.
    /// </summary>
    public static ExchangeRate Record(
        CurrencyPair pair,
        decimal      officialRate,
        decimal      spread,
        RateSource   source,
        int          validForMinutes = 60)
    {
        if (officialRate <= 0)
            throw new CurrencyDomainException($"Official rate must be positive. Got: {officialRate}.");
 
        if (spread < 0 || spread > 0.02m)
            throw new CurrencyDomainException($"Spread must be between 0% and 2%. Got: {spread:P2}.");
 
        var effectiveRate = officialRate * (1 - spread);
 
        if (effectiveRate <= 0)
            throw new CurrencyDomainException("Effective rate must be positive after spread.");
 
        var now = DateTimeOffset.UtcNow;
 
        return new ExchangeRate
        {
            Id            = Guid.NewGuid(),
            Pair          = pair,
            OfficialRate  = decimal.Round(officialRate, 8),
            Spread        = spread,
            EffectiveRate = decimal.Round(effectiveRate, 8),
            Source        = source,
            RecordedAt    = now,
            ValidUntil    = now.AddMinutes(validForMinutes),
        };
    }
 
    /// <summary>
    /// Crée le taux EUR/XOF fixe officiel (aucun appel API nécessaire).
    /// </summary>
    public static ExchangeRate CreateEurXofFixed(decimal spread = 0.005m) =>
        Record(
            CurrencyPair.EurXof,
            EurXofFixedRate,
            spread,
            RateSource.Fixed,
            validForMinutes: 60 * 24 * 365);   // 1 an — taux fixe

 
    /// <summary>
    /// Convertit un montant en utilisant le taux effectif.
    /// Le résultat est arrondi à l'entier inférieur (unités minimales).
    /// </summary>
    public long Convert(long amount) => (long)Math.Floor(amount * EffectiveRate);
 
    /// <summary>
    /// Convertit avec le taux officiel brut (sans spread).
    /// Utilisé pour les réconciliations comptables.
    /// </summary>
    public long ConvertAtOfficialRate(long amount) => (long)Math.Floor(amount * OfficialRate);
 
    /// <summary>
    /// Calcule le montant du spread en unités de destination.
    /// Utilisé pour afficher le coût de la conversion au marchand.
    /// </summary>
    public long SpreadAmount(long amount) => ConvertAtOfficialRate(amount) - Convert(amount);
 
    /// <summary>
    /// Retourne un résumé lisible de la conversion pour les logs.
    /// </summary>
    public string Describe(long amount) =>
        $"{amount} {Pair.From} → {Convert(amount)} {Pair.To} " +
        $"(taux officiel: {OfficialRate}, spread: {Spread:P2}, " +
        $"taux effectif: {EffectiveRate}, spread prélevé: {SpreadAmount(amount)} {Pair.To})";
}