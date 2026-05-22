using AfriPay.Application.Common.Errors;
using AfriPay.Application.Disputes.Dtos;
using AfriPay.Domain.Disputes;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Disputes.Queries;

public sealed record ListDisputesQuery(
    Guid    MerchantId,
    string? Status   = null,
    int     Page     = 1,
    int     PageSize = 20);
 
public sealed class ListDisputesHandler(IUnitOfWork uow)
{
    public async Task<Result<IReadOnlyList<DisputeDto>>> HandleAsync(
        ListDisputesQuery query, CancellationToken ct = default)
    {
        DisputeStatus? status = query.Status is not null
                                && Enum.TryParse<DisputeStatus>(query.Status, ignoreCase: true, out var s)
            ? s : null;
 
        var disputes = await uow.Disputes.GetByMerchantAsync(
            query.MerchantId, status,
            Math.Clamp(query.Page,     1, int.MaxValue),
            Math.Clamp(query.PageSize, 1, 100), ct);
 
        var dtos = (IReadOnlyList<DisputeDto>)disputes
            .Select(DisputeDto.FromDomain).ToList();
 
        return Result<IReadOnlyList<DisputeDto>>.Ok(dtos);
    }
}