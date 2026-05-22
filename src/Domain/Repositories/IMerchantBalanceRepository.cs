using AfriPay.Domain.Balance;

namespace AfriPay.Domain.Repositories;

public interface IMerchantBalanceRepository
{
    Task<MerchantBalance?> GetByMerchantAndCurrencyAsync(Guid merchantId, string currency, CancellationToken ct = default);
 
    Task<IReadOnlyList<MerchantBalance>> GetAllByMerchantAsync(Guid merchantId, CancellationToken ct = default);
 
    Task AddAsync(MerchantBalance balance, CancellationToken ct = default);
    void Update(MerchantBalance balance);
}