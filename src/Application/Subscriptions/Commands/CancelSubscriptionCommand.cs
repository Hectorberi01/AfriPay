using AfriPay.Application.Common.Errors;
using AfriPay.Application.Subscriptions.Dtos;
using AfriPay.Domain.Repositories;
using AfriPay.Domain.Subscriptions;

namespace AfriPay.Application.Subscriptions.Commands;

public sealed record CancelSubscriptionCommand(
    Guid MerchantId, Guid SubscriptionId, bool Immediately = false);
 
public sealed class CancelSubscriptionHandler(IUnitOfWork uow)
{
    public async Task<Result<SubscriptionDto>> HandleAsync(
        CancelSubscriptionCommand cmd, CancellationToken ct = default)
    {
        var sub = await uow.Subscriptions.GetByIdAsync(cmd.SubscriptionId, ct);
 
        if (sub is null)
            return Result<SubscriptionDto>.Fail(
                AppError.NotFound("Subscription", cmd.SubscriptionId.ToString()));
 
        if (sub.MerchantId != cmd.MerchantId)
            return Result<SubscriptionDto>.Fail(AppError.Unauthorized());
 
        try { sub.Cancel(cmd.Immediately); }
        catch (SubscriptionDomainException ex)
        {
            return Result<SubscriptionDto>.Fail(AppError.DomainRule(ex.Message));
        }
 
        uow.Subscriptions.Update(sub);
        await uow.SaveChangesAsync(ct);
 
        return Result<SubscriptionDto>.Ok(SubscriptionDto.FromDomain(sub));
    }
}