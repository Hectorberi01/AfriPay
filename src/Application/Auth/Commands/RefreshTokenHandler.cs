using AfriPay.Application.Auth.Dtos;
using AfriPay.Application.Auth.Interfaces;
using AfriPay.Application.Common.Errors;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Auth.Commands;

public sealed record RefreshTokenCommand(string RefreshToken);

public sealed class RefreshTokenHandler(
    IUnitOfWork   uow,
    ITokenService tokens)
{
    public async Task<Result<TokenRefreshDto>> HandleAsync(
        RefreshTokenCommand cmd, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cmd.RefreshToken))
            return Result<TokenRefreshDto>.Fail(
                AppError.Validation("refreshToken", "Refresh token is required."));
 
        var merchant = await uow.Merchants.GetByRefreshTokenAsync(cmd.RefreshToken, ct);
 
        if (merchant is null || !merchant.HasValidRefreshToken(cmd.RefreshToken))
            return Result<TokenRefreshDto>.Fail(
                AppError.Unauthorized("Invalid or expired refresh token.","401"));
 
        // Rotation du refresh token
        var newAccessToken  = tokens.GenerateAccessToken(
            merchant.Id, merchant.Email, merchant.Plan.ToString().ToLower());
        var newRefreshToken = tokens.GenerateRefreshToken();
 
        merchant.SetRefreshToken(newRefreshToken, DateTimeOffset.UtcNow.AddDays(7));
        uow.Merchants.Update(merchant);
        await uow.SaveChangesAsync(ct);
 
        return Result<TokenRefreshDto>.Ok(new TokenRefreshDto(
            newAccessToken, newRefreshToken, 15 * 60));
    }
}
