using AfriPay.Domain.Balance;
using AfriPay.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AfriPay.Infrastructure.Persistence.Repositories;

internal sealed class BalanceEntryRepository(AfriPayDbContextBase db) : IBalanceEntryRepository
{
    public async Task AddAsync(BalanceEntry entry, CancellationToken ct = default)
        => await db.Set<BalanceEntry>().AddAsync(entry, ct);
 
    public async Task<IReadOnlyList<BalanceEntry>> GetByMerchantAsync(
        Guid            merchantId,
        string          currency,
        DateTimeOffset? from     = null,
        DateTimeOffset? to       = null,
        int             page     = 1,
        int             pageSize = 50,
        CancellationToken ct     = default)
    {
        var query = db.Set<BalanceEntry>()
            .Where(e => e.MerchantId == merchantId
                        && e.Currency   == currency.ToUpperInvariant());
 
        if (from.HasValue) query = query.Where(e => e.CreatedAt >= from.Value);
        if (to.HasValue)   query = query.Where(e => e.CreatedAt <= to.Value);
 
        return await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }
    
    public async Task<long> GetTotalCreditAsync(
        Guid merchantId, string currency, CancellationToken ct = default)
        => await db.Set<BalanceEntry>()
            .Where(e => e.MerchantId == merchantId
                        && e.Currency   == currency.ToUpperInvariant()
                        && e.Type       == EntryType.Credit)
            .SumAsync(e => e.Amount, ct);
 
    public async Task<long> GetTotalDebitAsync(
        Guid merchantId, string currency, CancellationToken ct = default)
        => await db.Set<BalanceEntry>()
            .Where(e => e.MerchantId == merchantId
                        && e.Currency   == currency.ToUpperInvariant()
                        && e.Type       == EntryType.Debit)
            .SumAsync(e => e.Amount, ct);
}