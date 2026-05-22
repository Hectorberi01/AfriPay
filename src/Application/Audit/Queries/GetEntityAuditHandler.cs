using AfriPay.Application.Audit.Dtos;
using AfriPay.Application.Common.Errors;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Audit.Queries;

public sealed record GetEntityAuditQuery(string EntityType, string EntityId);

public sealed class GetEntityAuditHandler(IUnitOfWork uow)
{
    public async Task<Result<IReadOnlyList<AuditEntryDto>>> HandleAsync(
        GetEntityAuditQuery query, CancellationToken ct = default)
    {
        var entries = await uow.Audit.GetByEntityAsync(
            query.EntityType, query.EntityId, ct);
 
        var dtos = (IReadOnlyList<AuditEntryDto>)entries
            .Select(AuditEntryDto.FromDomain).ToList();
 
        return Result<IReadOnlyList<AuditEntryDto>>.Ok(dtos);
    }
}