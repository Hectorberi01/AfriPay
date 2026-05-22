using AfriPay.Domain.Disputes;

namespace AfriPay.Domain.Repositories;

public interface IDisputeRepository
{
    Task<Dispute?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Dispute>> GetByMerchantAsync(
        Guid merchantId, DisputeStatus? status,
        int page, int pageSize, CancellationToken ct = default);

    Task<IReadOnlyList<Dispute>> GetOverdueAsync(CancellationToken ct = default);

    Task AddAsync(Dispute dispute, CancellationToken ct = default);

    void Update(Dispute dispute);
}