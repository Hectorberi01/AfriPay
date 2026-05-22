using AfriPay.Domain.Balance;

namespace AfriPay.Domain.Repositories;

public interface IBalanceEntryRepository
{
    Task AddAsync(BalanceEntry entry, CancellationToken ct = default);
 
    Task<IReadOnlyList<BalanceEntry>> GetByMerchantAsync(
        Guid            merchantId,
        string          currency,
        DateTimeOffset? from     = null,
        DateTimeOffset? to       = null,
        int             page     = 1,
        int             pageSize = 50,
        CancellationToken ct     = default);
 
    Task<long> GetTotalCreditAsync(Guid merchantId, string currency, CancellationToken ct = default);
 
    Task<long> GetTotalDebitAsync(Guid merchantId, string currency, CancellationToken ct = default);
}