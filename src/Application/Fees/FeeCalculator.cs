using AfriPay.Domain.Fee;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Fees;

public interface IFeeCalculator
{
    Task<FeeCalculation> CalculateAsync(
        string            providerKey,
        string            plan,
        long              amount,
        string            currency,
        CancellationToken ct = default);
}

/// <summary>
/// Calcule la commission AfriPay pour un paiement.
///
/// Priorité :
///   1. Règle spécifique en base (provider + plan + currency)
///   2. Règle générique (provider + * + currency)
///   3. Taux par défaut codé dans DefaultFeeRates
/// </summary>
public sealed class FeeCalculator(IFeeRuleRepository feeRules) : IFeeCalculator
{
    public async Task<FeeCalculation> CalculateAsync(
        string            providerKey,
        string            plan,
        long              amount,
        string            currency,
        CancellationToken ct = default)
    {
        // 1. Chercher une règle spécifique en base
        var rules = await feeRules.GetActiveRulesAsync(providerKey, plan, currency, ct);
 
        var matchingRule = rules
            .Where(r => r.AppliesTo(providerKey, plan, currency, amount))
            .OrderByDescending(r => r.EffectiveFrom) // la plus récente en priorité
            .FirstOrDefault();
 
        if (matchingRule is not null)
            return FeeCalculation.From(matchingRule, amount, currency);
 
        // 2. Fallback sur les taux par défaut
        var defaultRate = DefaultFeeRates.ForProvider(providerKey);
        var fee         = (long)Math.Ceiling(amount * defaultRate / 100m);
 
        return new FeeCalculation(
            FeeAmount:       fee,
            NetAmount:       amount - fee,
            RateApplied:     defaultRate,
            Currency:        currency,
            RuleDescription: $"Default rate {defaultRate}% for {providerKey}");
    }
}