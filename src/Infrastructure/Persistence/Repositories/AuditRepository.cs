using AfriPay.Domain.Audit;
using AfriPay.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AfriPay.Infrastructure.Persistence.Repositories;

internal sealed class AuditRepository(AfriPayDbContextBase db) : IAuditRepository
{
    public async Task AddAsync(AuditEntry entry, CancellationToken ct)
        => await db.Set<AuditEntry>().AddAsync(entry, ct);
 
    public async Task<IReadOnlyList<AuditEntry>> GetByMerchantAsync(
        Guid merchantId, AuditAction? action,
        DateTimeOffset? from, DateTimeOffset? to,
        int page, int pageSize, CancellationToken ct)
    {
        var q = db.Set<AuditEntry>().Where(e => e.MerchantId == merchantId);
        if (action.HasValue) q = q.Where(e => e.Action == action.Value);
        if (from.HasValue)   q = q.Where(e => e.OccurredAt >= from.Value);
        if (to.HasValue)     q = q.Where(e => e.OccurredAt <= to.Value);
 
        return await q.OrderByDescending(e => e.OccurredAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    }
 
    public async Task<IReadOnlyList<AuditEntry>> GetByEntityAsync(
        string entityType, string entityId, CancellationToken ct)
        => await db.Set<AuditEntry>()
            .Where(e => e.EntityType == entityType && e.EntityId == entityId)
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync(ct);
}