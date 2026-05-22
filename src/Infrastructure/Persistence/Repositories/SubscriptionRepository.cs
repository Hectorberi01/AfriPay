using AfriPay.Domain.Repositories;
using AfriPay.Domain.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace AfriPay.Infrastructure.Persistence.Repositories;

internal sealed class SubscriptionRepository(AfriPayDbContextBase db)
    : ISubscriptionRepository
{
    public async Task<Subscription?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Set<Subscription>().FirstOrDefaultAsync(s => s.Id == id, ct);
 
    public async Task<IReadOnlyList<Subscription>> GetByMerchantAsync(
        Guid merchantId, SubscriptionStatus? status, int page, int pageSize,
        CancellationToken ct = default)
    {
        var q = db.Set<Subscription>().Where(s => s.MerchantId == merchantId);
        if (status.HasValue) q = q.Where(s => s.Status == status.Value);
        return await q.OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    }
 
    public async Task<IReadOnlyList<Subscription>> GetDueForRenewalAsync(CancellationToken ct = default)
        => await db.Set<Subscription>()
            .Where(s =>
                (s.Status == SubscriptionStatus.Active     && s.CurrentPeriodEnd <= DateTimeOffset.UtcNow) ||
                (s.Status == SubscriptionStatus.PastDue    && s.NextRetryAt     <= DateTimeOffset.UtcNow))
            .ToListAsync(ct);
 
    public async Task AddAsync(Subscription sub, CancellationToken ct = default)
        => await db.Set<Subscription>().AddAsync(sub, ct);
 
    public void Update(Subscription sub)
        => db.Set<Subscription>().Update(sub);
}