using AfriPay.Application.Auth.Dtos;
using AfriPay.Application.Auth.Interfaces;
using AfriPay.Application.Common.Errors;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Auth.Commands;

public sealed record LoginCommand(string Email, string Password);

public sealed class LoginHandler(IUnitOfWork uow, IPasswordService pwd, ITokenService   tokens)
{
    public async Task<Result<LoginDto>> HandleAsync(
        LoginCommand cmd, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cmd.Email) || string.IsNullOrWhiteSpace(cmd.Password))
            return Result<LoginDto>.Fail(
                AppError.Validation("credentials", "Email and password are required."));
 
        var merchant = await uow.Merchants.GetByEmailAsync(cmd.Email.ToLowerInvariant(), ct);
 
        // Message générique — ne pas révéler si l'email existe
        if (merchant is null || merchant.PasswordHash is null)
            return Result<LoginDto>.Fail(
                AppError.Unauthorized("Invalid credentials.","401"));
 
        if (!pwd.Verify(cmd.Password, merchant.PasswordHash))
            return Result<LoginDto>.Fail(
                AppError.Unauthorized("Invalid credentials.","401"));
 
        if (merchant.Status == Domain.Merchants.MerchantStatus.Suspended)
            return Result<LoginDto>.Fail(
                AppError.Unauthorized("Account suspended. Contact support@afripay.io.","401"));
 
        // Générer les tokens
        var accessToken  = tokens.GenerateAccessToken(
            merchant.Id, merchant.Email, merchant.Plan.ToString().ToLower());
        var refreshToken = tokens.GenerateRefreshToken();
 
        merchant.SetRefreshToken(refreshToken, DateTimeOffset.UtcNow.AddDays(7));
        uow.Merchants.Update(merchant);
        await uow.SaveChangesAsync(ct);
 
        return Result<LoginDto>.Ok(new LoginDto(
            AccessToken:  accessToken,
            RefreshToken: refreshToken,
            ExpiresIn:    15 * 60,   // 15 minutes en secondes
            TokenType:    "Bearer",
            MerchantId:   merchant.Id.ToString(),
            BusinessName: merchant.BusinessName,
            Email:        merchant.Email,
            Plan:         merchant.Plan.ToString().ToLower()));
    }
}