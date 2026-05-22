using AfriPay.Domain.Kyb;
using AfriPay.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AfriPay.Infrastructure.Persistence.Repositories;

internal sealed class KybRepository(AfriPayDbContextBase db)
    : IKybRepository
{
    public async Task<KybApplication?> GetByMerchantAsync(
        Guid merchantId, CancellationToken ct = default)
        => await db.Set<KybApplication>()
            .Include(k => k.Documents)
            .FirstOrDefaultAsync(k => k.MerchantId == merchantId, ct);
 
    public async Task<KybApplication?> GetByIdAsync(
        Guid id, CancellationToken ct = default)
        => await db.Set<KybApplication>()
            .Include(k => k.Documents)
            .FirstOrDefaultAsync(k => k.Id == id, ct);
 
    public async Task<IReadOnlyList<KybApplication>> GetByStatusAsync(
        KybStatus status, int page, int pageSize,
        CancellationToken ct = default)
        => await db.Set<KybApplication>()
            .Include(k => k.Documents)
            .Where(k => k.Status == status)
            .OrderByDescending(k => k.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
 
    public async Task AddAsync(KybApplication kyb, CancellationToken ct = default)
        => await db.Set<KybApplication>().AddAsync(kyb, ct);
 
    public void Update(KybApplication kyb)
        => db.Set<KybApplication>().Update(kyb);
}