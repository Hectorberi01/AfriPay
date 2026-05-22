using AfriPay.Application.Common.Errors;
using AfriPay.Application.Subscriptions.Dtos;
using AfriPay.Domain.Repositories;
using AfriPay.Domain.Subscriptions;

namespace AfriPay.Application.Subscriptions.Queries;

public sealed record ListSubscriptionsQuery(
    Guid MerchantId, string? Status = null, int Page = 1, int PageSize = 20);
 
public sealed class ListSubscriptionsHandler(IUnitOfWork uow)
{
    public async Task<Result<IReadOnlyList<SubscriptionDto>>> HandleAsync(
        ListSubscriptionsQuery query, CancellationToken ct = default)
    {
        SubscriptionStatus? status = query.Status is not null
                                     && Enum.TryParse<SubscriptionStatus>(query.Status, ignoreCase: true, out var s)
            ? s : null;
 
        var subs = await uow.Subscriptions.GetByMerchantAsync(
            query.MerchantId, status,
            Math.Clamp(query.Page, 1, int.MaxValue),
            Math.Clamp(query.PageSize, 1, 100), ct);
 
        var dtos = (IReadOnlyList<SubscriptionDto>)subs
            .Select(SubscriptionDto.FromDomain).ToList();
 
        return Result<IReadOnlyList<SubscriptionDto>>.Ok(dtos);
    }
}