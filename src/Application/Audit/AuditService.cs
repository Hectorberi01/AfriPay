using AfriPay.Application.Audit.Dtos;
using AfriPay.Application.Audit.Queries;
using AfriPay.Application.Common.Errors;

namespace AfriPay.Application.Audit;

public interface IAuditService
{
    Task<Result<IReadOnlyList<AuditEntryDto>>> ListAsync(
        ListAuditEntriesQuery query, CancellationToken ct = default);
 
    Task<Result<IReadOnlyList<AuditEntryDto>>> GetEntityHistoryAsync(
        GetEntityAuditQuery query, CancellationToken ct = default);
}
 
public sealed class AuditService(
    ListAuditEntriesHandler listHandler,
    GetEntityAuditHandler   entityHandler) : IAuditService
{
    public Task<Result<IReadOnlyList<AuditEntryDto>>> ListAsync(
        ListAuditEntriesQuery query, CancellationToken ct = default)
        => listHandler.HandleAsync(query, ct);
 
    public Task<Result<IReadOnlyList<AuditEntryDto>>> GetEntityHistoryAsync(
        GetEntityAuditQuery query, CancellationToken ct = default)
        => entityHandler.HandleAsync(query, ct);
}