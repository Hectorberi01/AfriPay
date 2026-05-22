using AfriPay.Domain.Kyb;

namespace AfriPay.Domain.Repositories;

public interface IKybRepository
{
    Task<KybApplication?> GetByMerchantAsync(Guid merchantId, CancellationToken ct = default);
 
    Task<KybApplication?> GetByIdAsync(Guid id, CancellationToken ct = default);
 
    Task<IReadOnlyList<KybApplication>> GetByStatusAsync(KybStatus status, int page, int pageSize, CancellationToken ct = default);
 
    Task AddAsync(KybApplication kyb, CancellationToken ct = default);
    void Update(KybApplication kyb);
}