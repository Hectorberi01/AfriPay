using AfriPay.Domain.Fee;
using AfriPay.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AfriPay.Infrastructure.Persistence.Repositories;

internal sealed class FeeRuleRepository(AfriPayDbContextBase db) : IFeeRuleRepository
{
    public async Task<IReadOnlyList<FeeRule>> GetActiveRulesAsync(
        string providerKey, string plan, string currency,
        CancellationToken ct = default)
        => await db.Set<FeeRule>()
            .Where(r => r.IsActive
                        && r.EffectiveFrom <= DateTimeOffset.UtcNow
                        && (r.EffectiveTo == null || r.EffectiveTo >= DateTimeOffset.UtcNow)
                        && (r.ProviderKey == "*" || r.ProviderKey == providerKey)
                        && (r.Plan        == "*" || r.Plan        == plan)
                        && (r.Currency    == "*" || r.Currency    == currency))
            .OrderByDescending(r => r.EffectiveFrom)
            .ToListAsync(ct);
 
    public async Task AddAsync(FeeRule rule, CancellationToken ct = default)
        => await db.Set<FeeRule>().AddAsync(rule, ct);
 
    public void Update(FeeRule rule)
        => db.Set<FeeRule>().Update(rule);
}