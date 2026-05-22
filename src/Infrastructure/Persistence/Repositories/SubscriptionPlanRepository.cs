using AfriPay.Domain.Repositories;
using AfriPay.Domain.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace AfriPay.Infrastructure.Persistence.Repositories;

internal sealed class SubscriptionPlanRepository(AfriPayDbContextBase db)
    : ISubscriptionPlanRepository
{
    public async Task<SubscriptionPlan?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Set<SubscriptionPlan>().FirstOrDefaultAsync(p => p.Id == id, ct);
 
    public async Task<IReadOnlyList<SubscriptionPlan>> GetByMerchantAsync(
        Guid merchantId, CancellationToken ct = default)
        => await db.Set<SubscriptionPlan>()
            .Where(p => p.MerchantId == merchantId)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(ct);
 
    public async Task AddAsync(SubscriptionPlan plan, CancellationToken ct = default)
        => await db.Set<SubscriptionPlan>().AddAsync(plan, ct);
 
    public void Update(SubscriptionPlan plan)
        => db.Set<SubscriptionPlan>().Update(plan);
}