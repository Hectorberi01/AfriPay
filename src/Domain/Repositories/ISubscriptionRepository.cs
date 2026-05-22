using AfriPay.Domain.Subscriptions;

namespace AfriPay.Domain.Repositories;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Subscription>> GetByMerchantAsync(
        Guid merchantId, SubscriptionStatus? status, int page, int pageSize,
        CancellationToken ct = default);

    Task<IReadOnlyList<Subscription>> GetDueForRenewalAsync(CancellationToken ct = default);

    Task AddAsync(Subscription sub, CancellationToken ct = default);

    void Update(Subscription sub);
}