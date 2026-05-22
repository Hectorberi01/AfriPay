using AfriPay.Domain.Currency;
using AfriPay.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AfriPay.Infrastructure.Persistence.Repositories;

internal sealed class ExchangeRateRepository(AfriPayDbContextBase db)
    : Repository<ExchangeRate>(db), IExchangeRateRepository
{
    public async Task<ExchangeRate?> GetLatestValidAsync(
        CurrencyPair pair, CancellationToken ct = default)
        => await Db.ExchangeRates
            .Where(e => e.Pair.From == pair.From
                        && e.Pair.To   == pair.To
                        && e.ValidUntil > DateTimeOffset.UtcNow)
            .OrderByDescending(e => e.RecordedAt)
            .FirstOrDefaultAsync(ct);
 
    public async Task<ExchangeRate?> GetLastRecordedAsync(
        CurrencyPair pair, CancellationToken ct = default)
        => await Db.ExchangeRates
            .Where(e => e.Pair.From == pair.From
                        && e.Pair.To   == pair.To)
            .OrderByDescending(e => e.RecordedAt)
            .FirstOrDefaultAsync(ct);
 
    public async Task<IReadOnlyList<ExchangeRate>> GetHistoryAsync(
        CurrencyPair pair, DateTimeOffset since,
        DateTimeOffset? until = null, CancellationToken ct = default)
    {
        var query = Db.ExchangeRates
            .Where(e => e.Pair.From    == pair.From
                        && e.Pair.To      == pair.To
                        && e.RecordedAt   >= since);
 
        if (until is not null)
            query = query.Where(e => e.RecordedAt <= until);
 
        return await query
            .OrderByDescending(e => e.RecordedAt)
            .ToListAsync(ct);
    }
 
    public async Task<ExchangeRate?> GetAtInstantAsync(
        CurrencyPair pair, DateTimeOffset at, CancellationToken ct = default)
        => await Db.ExchangeRates
            .Where(e => e.Pair.From  == pair.From
                        && e.Pair.To    == pair.To
                        && e.RecordedAt <= at)
            .OrderByDescending(e => e.RecordedAt)
            .FirstOrDefaultAsync(ct);
}