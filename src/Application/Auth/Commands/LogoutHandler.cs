using AfriPay.Application.Common.Errors;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Auth.Commands;

public sealed record LogoutCommand(Guid MerchantId);

public sealed class LogoutHandler(IUnitOfWork uow)
{
    public async Task<Result<bool>> HandleAsync(
        LogoutCommand cmd, CancellationToken ct = default)
    {
        var merchant = await uow.Merchants.GetByIdAsync(cmd.MerchantId, ct);
        if (merchant is null)
            return Result<bool>.Ok(true); // Idempotent
 
        merchant.RevokeRefreshToken();
        uow.Merchants.Update(merchant);
        await uow.SaveChangesAsync(ct);
 
        return Result<bool>.Ok(true);
    }
}
