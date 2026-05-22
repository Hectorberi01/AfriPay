using AfriPay.Domain.Balance;
using AfriPay.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AfriPay.Infrastructure.Persistence.Repositories;

internal sealed class MerchantBalanceRepository(AfriPayDbContextBase db)
    : IMerchantBalanceRepository
{
    public async Task<MerchantBalance?> GetByMerchantAndCurrencyAsync(
        Guid merchantId, string currency, CancellationToken ct = default)
        => await db.Set<MerchantBalance>()
            .FirstOrDefaultAsync(
                b => b.MerchantId == merchantId
                     && b.Currency == currency.ToUpperInvariant(), ct);
 
    public async Task<IReadOnlyList<MerchantBalance>> GetAllByMerchantAsync(
        Guid merchantId, CancellationToken ct = default)
        => await db.Set<MerchantBalance>()
            .Where(b => b.MerchantId == merchantId)
            .OrderBy(b => b.Currency)
            .ToListAsync(ct);
 
    public async Task AddAsync(MerchantBalance balance, CancellationToken ct = default)
        => await db.Set<MerchantBalance>().AddAsync(balance, ct);
 
    public void Update(MerchantBalance balance)
        => db.Set<MerchantBalance>().Update(balance);
}