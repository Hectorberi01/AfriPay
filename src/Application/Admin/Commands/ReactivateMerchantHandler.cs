using AfriPay.Application.Common.Errors;
using AfriPay.Domain.Audit;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Admin.Commands;

public sealed record ReactivateMerchantCommand(Guid MerchantId, string AdminEmail);
 
public sealed class ReactivateMerchantHandler(IUnitOfWork uow)
{
    public async Task<Result<bool>> HandleAsync(
        ReactivateMerchantCommand cmd, CancellationToken ct = default)
    {
        var merchant = await uow.Merchants.GetByIdAsync(cmd.MerchantId, ct);
        if (merchant is null)
            return Result<bool>.Fail(
                AppError.NotFound("Merchant", cmd.MerchantId.ToString()));
 
        merchant.Reactivate();
        uow.Merchants.Update(merchant);
 
        await uow.Audit.AddAsync(AuditEntry.Create(
            cmd.MerchantId, AuditAction.AdminAction,
            "Merchant", cmd.MerchantId.ToString(),
            actorId: cmd.AdminEmail, actorType: "admin",
            metadata: "{\"action\":\"reactivate\"}"), ct);
 
        await uow.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }
}