namespace AfriPay.Domain.Merchants.ValueObjects;

/// <summary>
/// Limites opérationnelles d'un marchand, calculées à partir de son <see cref="PricingPlan"/>.
/// Valeur immuable : recalculée à chaque changement de plan via <see cref="ForPlan"/>.
/// </summary>
/// <param name="MonthlyTransactionLimit">Nombre maximum de transactions autorisées par mois calendaire.</param>
/// <param name="RatePerMinute">Nombre maximum de requêtes d'initiation de paiement par minute.</param>
/// <param name="CanAccessAllProviders">Indique si le marchand peut router vers tous les fournisseurs de paiement disponibles.</param>
public sealed record MerchantLimits(int MonthlyTransactionLimit, int RatePerMinute, bool CanAccessAllProviders)
{
    /// <summary>Retourne les limites correspondant au plan tarifaire donné.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Levée si le plan est inconnu.</exception>
    public static MerchantLimits ForPlan(PricingPlan plan) => plan switch
    {
        PricingPlan.Starter => new(500, 100, false),
        PricingPlan.Growth  => new(5_000, 1_000, true),
        PricingPlan.Scale   => new(int.MaxValue, int.MaxValue, true),
        _                   => throw new ArgumentOutOfRangeException(nameof(plan)),
    };
}