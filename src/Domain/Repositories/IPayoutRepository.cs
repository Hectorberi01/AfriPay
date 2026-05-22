using AfriPay.Domain.Payouts;

namespace AfriPay.Domain.Repositories;

public interface IPayoutRepository
{
    Task<Payout?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Payout>> GetByMerchantAsync(
        Guid merchantId, PayoutStatus? status, int page, int pageSize, CancellationToken ct);
    Task<IReadOnlyList<Payout>> GetPendingAsync(CancellationToken ct);
    Task AddAsync(Payout payout, CancellationToken ct);
    void Update(Payout payout);
}