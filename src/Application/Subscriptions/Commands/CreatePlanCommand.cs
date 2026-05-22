using AfriPay.Application.Common.Errors;
using AfriPay.Application.Subscriptions.Dtos;
using AfriPay.Domain.Repositories;
using AfriPay.Domain.Subscriptions;

namespace AfriPay.Application.Subscriptions.Commands;

public sealed record CreatePlanCommand(
    Guid   MerchantId, string Name, long Amount, string Currency,
    string Interval, string ProviderKey, int IntervalCount = 1,
    int TrialDays = 0, string? Description = null);
 
public sealed class CreatePlanHandler(IUnitOfWork uow)
{
    public async Task<Result<SubscriptionPlanDto>> HandleAsync(
        CreatePlanCommand cmd, CancellationToken ct = default)
    {
        if (!Enum.TryParse<BillingInterval>(cmd.Interval, ignoreCase: true, out var interval))
            return Result<SubscriptionPlanDto>.Fail(
                AppError.Validation("interval",
                    $"Invalid interval. Valid: {string.Join(", ", Enum.GetNames<BillingInterval>())}."));
 
        SubscriptionPlan plan;
        try
        {
            plan = SubscriptionPlan.Create(
                cmd.MerchantId, cmd.Name, cmd.Amount, cmd.Currency,
                interval, cmd.ProviderKey, cmd.IntervalCount,
                cmd.TrialDays, cmd.Description);
        }
        catch (SubscriptionDomainException ex)
        {
            return Result<SubscriptionPlanDto>.Fail(AppError.DomainRule(ex.Message));
        }
 
        await uow.SubscriptionPlans.AddAsync(plan, ct);
        await uow.SaveChangesAsync(ct);
 
        return Result<SubscriptionPlanDto>.Ok(SubscriptionPlanDto.FromDomain(plan));
    }
}