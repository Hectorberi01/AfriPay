using AfriPay.Application.Common.Errors;
using AfriPay.Domain.Audit;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Admin.Commands;

public sealed record SuspendMerchantCommand(
    Guid   MerchantId, string Reason, string AdminEmail);
 
public sealed class SuspendMerchantHandler(IUnitOfWork uow)
{
    public async Task<Result<bool>> HandleAsync(
        SuspendMerchantCommand cmd, CancellationToken ct = default)
    {
        var merchant = await uow.Merchants.GetByIdAsync(cmd.MerchantId, ct);
        if (merchant is null)
            return Result<bool>.Fail(
                AppError.NotFound("Merchant", cmd.MerchantId.ToString()));
 
        merchant.Suspend();
        uow.Merchants.Update(merchant);
 
        await uow.Audit.AddAsync(AuditEntry.Create(
            cmd.MerchantId, AuditAction.AdminAction,
            "Merchant", cmd.MerchantId.ToString(),
            actorId: cmd.AdminEmail, actorType: "admin",
            metadata: $"{{\"action\":\"suspend\",\"reason\":\"{cmd.Reason}\"}}"), ct);
 
        await uow.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }
}