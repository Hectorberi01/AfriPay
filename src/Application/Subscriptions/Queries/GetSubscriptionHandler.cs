using AfriPay.Application.Common.Errors;
using AfriPay.Application.Subscriptions.Dtos;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Subscriptions.Queries;

public sealed record GetSubscriptionQuery(Guid MerchantId, Guid SubscriptionId);
 
public sealed class GetSubscriptionHandler(IUnitOfWork uow)
{
    public async Task<Result<SubscriptionDto>> HandleAsync(
        GetSubscriptionQuery query, CancellationToken ct = default)
    {
        var sub = await uow.Subscriptions.GetByIdAsync(query.SubscriptionId, ct);
        if (sub is null)
            return Result<SubscriptionDto>.Fail(
                AppError.NotFound("Subscription", query.SubscriptionId.ToString()));
        if (sub.MerchantId != query.MerchantId)
            return Result<SubscriptionDto>.Fail(AppError.Unauthorized());
        return Result<SubscriptionDto>.Ok(SubscriptionDto.FromDomain(sub));
    }
}
