using AfriPay.Domain.Audit;

namespace AfriPay.Domain.Repositories;

public interface IAuditRepository
{
    Task AddAsync(AuditEntry entry, CancellationToken ct);

    Task<IReadOnlyList<AuditEntry>> GetByMerchantAsync(
        Guid merchantId, AuditAction? action,
        DateTimeOffset? from, DateTimeOffset? to,
        int page, int pageSize, CancellationToken ct);

    Task<IReadOnlyList<AuditEntry>> GetByEntityAsync(
        string entityType, string entityId, CancellationToken ct);
}