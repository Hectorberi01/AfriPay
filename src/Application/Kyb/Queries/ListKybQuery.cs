using AfriPay.Application.Common.Errors;
using AfriPay.Application.Kyb.Dtos;
using AfriPay.Domain.Kyb;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Kyb.Queries;

public sealed record ListKybQuery(
    string? Status   = null,
    int     Page     = 1,
    int     PageSize = 20);
 
public sealed class ListKybHandler(IUnitOfWork uow)
{
    public async Task<Result<IReadOnlyList<KybDto>>> HandleAsync(
        ListKybQuery query, CancellationToken ct = default)
    {
        KybStatus? status = query.Status is not null
                            && Enum.TryParse<KybStatus>(query.Status, ignoreCase: true, out var s)
            ? s : null;
 
        IReadOnlyList<KybApplication> list;
 
        if (status.HasValue)
            list = await uow.Kyb.GetByStatusAsync(
                status.Value,
                Math.Clamp(query.Page,     1, int.MaxValue),
                Math.Clamp(query.PageSize, 1, 100),
                ct);
        else
            list = await uow.Kyb.GetByStatusAsync(
                KybStatus.Submitted,
                query.Page, query.PageSize, ct);
 
        var dtos = (IReadOnlyList<KybDto>)list
            .Select(KybDto.FromDomain).ToList();
 
        return Result<IReadOnlyList<KybDto>>.Ok(dtos);
    }
}