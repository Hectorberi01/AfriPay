using AfriPay.Application.Auth.Interfaces;
using AfriPay.Application.Common.Errors;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Auth.Commands;

public sealed record ChangePasswordCommand(Guid MerchantId, string CurrentPassword, string NewPassword);

public sealed class ChangePasswordHandler(
    IUnitOfWork      uow,
    IPasswordService pwd)
{
    public async Task<Result<bool>> HandleAsync(
        ChangePasswordCommand cmd, CancellationToken ct = default)
    {
        var merchant = await uow.Merchants.GetByIdAsync(cmd.MerchantId, ct);
        if (merchant is null)
            return Result<bool>.Fail(
                AppError.NotFound("Merchant", cmd.MerchantId.ToString()));
 
        if (merchant.PasswordHash is null || !pwd.Verify(cmd.CurrentPassword, merchant.PasswordHash))
            return Result<bool>.Fail(
                AppError.Unauthorized("Current password is incorrect.","401"));
 
        if (cmd.NewPassword.Length < 8)
            return Result<bool>.Fail(
                AppError.Validation("newPassword", "Password must be at least 8 characters."));
 
        merchant.SetPassword(pwd.Hash(cmd.NewPassword));
        merchant.RevokeRefreshToken(); // Force re-login
        uow.Merchants.Update(merchant);
        await uow.SaveChangesAsync(ct);
 
        return Result<bool>.Ok(true);
    }
}