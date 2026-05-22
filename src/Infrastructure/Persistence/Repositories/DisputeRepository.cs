using AfriPay.Domain.Disputes;
using AfriPay.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AfriPay.Infrastructure.Persistence.Repositories;

internal sealed class DisputeRepository(AfriPayDbContextBase db)
    : IDisputeRepository
{
    public async Task<Dispute?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Set<Dispute>()
            .Include(d => d.Evidence)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<IReadOnlyList<Dispute>> GetByMerchantAsync(
        Guid merchantId, DisputeStatus? status,
        int page, int pageSize, CancellationToken ct = default)
    {
        var q = db.Set<Dispute>().Where(d => d.MerchantId == merchantId);
        if (status.HasValue) q = q.Where(d => d.Status == status.Value);
        return await q
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Dispute>> GetOverdueAsync(CancellationToken ct = default)
        => await db.Set<Dispute>()
            .Where(d => d.Status == DisputeStatus.Open
                        && d.RespondBy <= DateTimeOffset.UtcNow)
            .ToListAsync(ct);

    public async Task AddAsync(Dispute dispute, CancellationToken ct = default)
        => await db.Set<Dispute>().AddAsync(dispute, ct);

    public void Update(Dispute dispute)
        => db.Set<Dispute>().Update(dispute);
}