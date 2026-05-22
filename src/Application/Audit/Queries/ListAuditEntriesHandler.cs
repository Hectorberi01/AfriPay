using AfriPay.Application.Audit.Dtos;
using AfriPay.Application.Common.Errors;
using AfriPay.Domain.Audit;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Audit.Queries;

public sealed record ListAuditEntriesQuery(
    Guid            MerchantId,
    string?         Action   = null,
    DateTimeOffset? From     = null,
    DateTimeOffset? To       = null,
    int             Page     = 1,
    int             PageSize = 50);

public sealed class ListAuditEntriesHandler(IUnitOfWork uow)
{
    public async Task<Result<IReadOnlyList<AuditEntryDto>>> HandleAsync(
        ListAuditEntriesQuery query, CancellationToken ct = default)
    {
        AuditAction? action = query.Action is not null
                              && Enum.TryParse<AuditAction>(query.Action, ignoreCase: true, out var a)
            ? a : null;
 
        var entries = await uow.Audit.GetByMerchantAsync(
            query.MerchantId, action,
            query.From, query.To,
            Math.Clamp(query.Page,     1, int.MaxValue),
            Math.Clamp(query.PageSize, 1, 200), ct);
 
        var dtos = (IReadOnlyList<AuditEntryDto>)entries
            .Select(AuditEntryDto.FromDomain).ToList();
 
        return Result<IReadOnlyList<AuditEntryDto>>.Ok(dtos);
    }
}