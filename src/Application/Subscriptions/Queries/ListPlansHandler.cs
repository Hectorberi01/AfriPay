using AfriPay.Application.Common.Errors;
using AfriPay.Application.Subscriptions.Dtos;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Subscriptions.Queries;

public sealed record ListPlansQuery(Guid MerchantId);
 
public sealed class ListPlansHandler(IUnitOfWork uow)
{
    public async Task<Result<IReadOnlyList<SubscriptionPlanDto>>> HandleAsync(
        ListPlansQuery query, CancellationToken ct = default)
    {
        var plans = await uow.SubscriptionPlans.GetByMerchantAsync(query.MerchantId, ct);
        var dtos  = (IReadOnlyList<SubscriptionPlanDto>)plans
            .Select(SubscriptionPlanDto.FromDomain).ToList();
        return Result<IReadOnlyList<SubscriptionPlanDto>>.Ok(dtos);
    }
}