using AfriPay.Application.Common.Errors;
using AfriPay.Domain.Audit;
using AfriPay.Domain.Merchants;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Admin.Commands;

public sealed record ChangePlanCommand(
    Guid   MerchantId, string Plan, string AdminEmail);
 
public sealed class ChangePlanHandler(IUnitOfWork uow)
{
    public async Task<Result<bool>> HandleAsync(
        ChangePlanCommand cmd, CancellationToken ct = default)
    {
        if (!Enum.TryParse<PricingPlan>(cmd.Plan, ignoreCase: true, out var plan))
            return Result<bool>.Fail(
                AppError.Validation("plan",
                    $"Valid plans: {string.Join(", ", Enum.GetNames<PricingPlan>())}."));
 
        var merchant = await uow.Merchants.GetByIdAsync(cmd.MerchantId, ct);
        if (merchant is null)
            return Result<bool>.Fail(
                AppError.NotFound("Merchant", cmd.MerchantId.ToString()));
 
        merchant.ChangePlan(plan);
        uow.Merchants.Update(merchant);
 
        await uow.Audit.AddAsync(AuditEntry.Create(
            cmd.MerchantId, AuditAction.PlanChanged,
            "Merchant", cmd.MerchantId.ToString(),
            actorId: cmd.AdminEmail, actorType: "admin",
            metadata: $"{{\"plan\":\"{plan}\"}}"), ct);
 
        await uow.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }
}