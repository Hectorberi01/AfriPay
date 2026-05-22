using AfriPay.Application.Common.Errors;
using AfriPay.Application.Subscriptions.Dtos;
using AfriPay.Domain.Repositories;
using AfriPay.Domain.Subscriptions;

namespace AfriPay.Application.Subscriptions.Commands;

public sealed record CreateSubscriptionCommand(
    Guid    MerchantId, Guid PlanId,
    string  CustomerPhone, string? CustomerEmail, string? CustomerName);
 
public sealed class CreateSubscriptionHandler(IUnitOfWork uow)
{
    public async Task<Result<SubscriptionDto>> HandleAsync(
        CreateSubscriptionCommand cmd, CancellationToken ct = default)
    {
        var plan = await uow.SubscriptionPlans.GetByIdAsync(cmd.PlanId, ct);
 
        if (plan is null)
            return Result<SubscriptionDto>.Fail(
                AppError.NotFound("SubscriptionPlan", cmd.PlanId.ToString()));
 
        if (plan.MerchantId != cmd.MerchantId)
            return Result<SubscriptionDto>.Fail(AppError.Unauthorized());
 
        if (!plan.IsActive)
            return Result<SubscriptionDto>.Fail(
                AppError.Conflict("Plan is no longer active."));
 
        Subscription sub;
        try { sub = Subscription.Create(cmd.MerchantId, plan, cmd.CustomerPhone, cmd.CustomerEmail, cmd.CustomerName); }
        catch (SubscriptionDomainException ex)
        {
            return Result<SubscriptionDto>.Fail(AppError.DomainRule(ex.Message));
        }
 
        await uow.Subscriptions.AddAsync(sub, ct);
        await uow.SaveChangesAsync(ct);
 
        return Result<SubscriptionDto>.Ok(SubscriptionDto.FromDomain(sub));
    }
}