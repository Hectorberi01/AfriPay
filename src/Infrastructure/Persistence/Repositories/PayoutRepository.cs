using AfriPay.Domain.Payouts;
using AfriPay.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AfriPay.Infrastructure.Persistence.Repositories;

internal sealed class PayoutRepository(AfriPayDbContextBase db) : IPayoutRepository
{
    public async Task<Payout?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Set<Payout>().FirstOrDefaultAsync(p => p.Id == id, ct);
 
    public async Task<IReadOnlyList<Payout>> GetByMerchantAsync(
        Guid merchantId, PayoutStatus? status, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Set<Payout>().Where(p => p.MerchantId == merchantId);
        if (status.HasValue) query = query.Where(p => p.Status == status.Value);
        return await query.OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    }
 
    public async Task<IReadOnlyList<Payout>> GetPendingAsync(CancellationToken ct)
        => await db.Set<Payout>()
            .Where(p => p.Status == PayoutStatus.Pending)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(ct);
 
    public async Task AddAsync(Payout payout, CancellationToken ct)
        => await db.Set<Payout>().AddAsync(payout, ct);
 
    public void Update(Payout payout)
        => db.Set<Payout>().Update(payout);
}