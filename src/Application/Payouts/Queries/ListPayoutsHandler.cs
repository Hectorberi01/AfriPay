using AfriPay.Application.Common.Errors;
using AfriPay.Application.Payouts.Dtos;
using AfriPay.Domain.Payouts;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Payouts.Queries;

public sealed record ListPayoutsQuery(
    Guid   MerchantId,
    string? Status   = null,
    int     Page     = 1,
    int     PageSize = 20);
 
public sealed class ListPayoutsHandler(IUnitOfWork uow)
{
    public async Task<Result<IReadOnlyList<PayoutDto>>> HandleAsync(
        ListPayoutsQuery query, CancellationToken ct = default)
    {
        PayoutStatus? status = query.Status is not null
                               && Enum.TryParse<PayoutStatus>(query.Status, ignoreCase: true, out var s)
            ? s : null;
 
        var payouts = await uow.Payouts.GetByMerchantAsync(
            query.MerchantId, status,
            Math.Clamp(query.Page,     1, int.MaxValue),
            Math.Clamp(query.PageSize, 1, 100), ct);
 
        var dtos = (IReadOnlyList<PayoutDto>)payouts
            .Select(PayoutDto.FromDomain).ToList();
 
        return Result<IReadOnlyList<PayoutDto>>.Ok(dtos);
    }
}