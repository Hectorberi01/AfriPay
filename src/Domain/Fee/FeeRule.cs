namespace AfriPay.Domain.Fee;

/// <summary>
/// Règle de commission AfriPay applicable à un paiement.
/// Configurable par : provider, plan tarifaire, devise, tranche de montant.
/// </summary>

public class FeeRule
{
    public Guid     Id           { get; private set; }
    public string   ProviderKey  { get; private set; } = default!;  // "mtn_momo", "stripe"…
    public string   Plan         { get; private set; } = default!;  // "starter", "growth", "scale"
    public string   Currency     { get; private set; } = default!;  // "XOF", "EUR", "*"
    public FeeType  Type         { get; private set; }
    public FeeTier  Tier         { get; private set; }
 
    // Taux (pour Percentage et Mixed)
    public decimal  RatePercent  { get; private set; }  // ex : 0.8 = 0.8%
 
    // Montant fixe (pour Fixed et Mixed)
    public long     FixedAmount  { get; private set; }  // ex : 50 XOF
 
    // Plafond de commission (optionnel)
    public long?    MaxFee       { get; private set; }
 
    // Tranches de volume
    public long     MinAmount    { get; private set; }  // Montant minimum pour cette règle
    public long     MaxAmount    { get; private set; }  // 0 = pas de plafond
 
    public bool     IsActive     { get; private set; }
    public DateTimeOffset EffectiveFrom { get; private set; }
    public DateTimeOffset? EffectiveTo  { get; private set; }
 
    private FeeRule() { }
    
    public static FeeRule CreatePercentage(
        string  providerKey,
        string  plan,
        string  currency,
        decimal ratePercent,
        FeeTier tier,
        long?   maxFee     = null,
        long    minAmount  = 0,
        long    maxAmount  = 0)
        => new()
        {
            Id            = Guid.NewGuid(),
            ProviderKey   = providerKey,
            Plan          = plan,
            Currency      = currency,
            Type          = FeeType.Percentage,
            Tier          = tier,
            RatePercent   = ratePercent,
            FixedAmount   = 0,
            MaxFee        = maxFee,
            MinAmount     = minAmount,
            MaxAmount     = maxAmount,
            IsActive      = true,
            EffectiveFrom = DateTimeOffset.UtcNow,
        };
    
    public static FeeRule CreateMixed(
        string  providerKey,
        string  plan,
        string  currency,
        decimal ratePercent,
        long    fixedAmount,
        FeeTier tier,
        long?   maxFee = null)
        => new()
        {
            Id            = Guid.NewGuid(),
            ProviderKey   = providerKey,
            Plan          = plan,
            Currency      = currency,
            Type          = FeeType.Mixed,
            Tier          = tier,
            RatePercent   = ratePercent,
            FixedAmount   = fixedAmount,
            MaxFee        = maxFee,
            IsActive      = true,
            EffectiveFrom = DateTimeOffset.UtcNow,
        };
    
    /// <summary>Calcule la commission pour un montant donné.</summary>
    public long Calculate(long amount)
    {
        long fee = Type switch
        {
            FeeType.Percentage => (long)Math.Ceiling(amount * RatePercent / 100m),
            FeeType.Fixed      => FixedAmount,
            FeeType.Mixed      => (long)Math.Ceiling(amount * RatePercent / 100m) + FixedAmount,
            _                  => 0,
        };
 
        // Appliquer le plafond si défini
        if (MaxFee.HasValue && fee > MaxFee.Value)
            fee = MaxFee.Value;
 
        return fee;
    }
    
    public bool AppliesTo(string providerKey, string plan, string currency, long amount)
    {
        if (!IsActive) return false;
        if (EffectiveTo.HasValue && DateTimeOffset.UtcNow > EffectiveTo) return false;
 
        var providerMatch  = ProviderKey == "*" || ProviderKey.Equals(providerKey, StringComparison.OrdinalIgnoreCase);
        var planMatch      = Plan        == "*" || Plan.Equals(plan,        StringComparison.OrdinalIgnoreCase);
        var currencyMatch  = Currency    == "*" || Currency.Equals(currency,  StringComparison.OrdinalIgnoreCase);
        var amountInRange  = amount >= MinAmount && (MaxAmount == 0 || amount <= MaxAmount);
 
        return providerMatch && planMatch && currencyMatch && amountInRange;
    }
 
    public void Deactivate()
    {
        IsActive      = false;
        EffectiveTo   = DateTimeOffset.UtcNow;
    }
}