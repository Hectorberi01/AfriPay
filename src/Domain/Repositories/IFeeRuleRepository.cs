using AfriPay.Domain.Fee;

namespace AfriPay.Domain.Repositories;

public interface IFeeRuleRepository
{
    Task<IReadOnlyList<FeeRule>> GetActiveRulesAsync(
        string providerKey, string plan, string currency,
        CancellationToken ct = default);
 
    Task AddAsync(FeeRule rule, CancellationToken ct = default);
    void Update(FeeRule rule);
}