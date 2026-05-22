using AfriPay.Domain.Subscriptions;

namespace AfriPay.Domain.Repositories;

public interface ISubscriptionPlanRepository
{
    Task<SubscriptionPlan?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<SubscriptionPlan>> GetByMerchantAsync(
        Guid merchantId, CancellationToken ct = default);

    Task AddAsync(SubscriptionPlan plan, CancellationToken ct = default);

    void Update(SubscriptionPlan plan);
}