using AfriPay.Application.Auth.Dtos;
using AfriPay.Application.Auth.Interfaces;
using AfriPay.Application.Common.Errors;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Auth.Commands;

public sealed record RegisterCommand(
    string BusinessName,
    string Email,
    string Password,
    string Country);

public sealed class RegisterHandler(
    IUnitOfWork      uow,
    IPasswordService pwd,
    ITokenService    tokens)
{
    public async Task<Result<RegisterDto>> HandleAsync(
        RegisterCommand cmd, CancellationToken ct = default)
    {
        // Validation basique
        if (string.IsNullOrWhiteSpace(cmd.BusinessName))
            return Result<RegisterDto>.Fail(
                AppError.Validation("businessName", "Business name is required."));
 
        if (string.IsNullOrWhiteSpace(cmd.Email) || !cmd.Email.Contains('@'))
            return Result<RegisterDto>.Fail(
                AppError.Validation("email", "Valid email is required."));
 
        if (cmd.Password.Length < 8)
            return Result<RegisterDto>.Fail(
                AppError.Validation("password", "Password must be at least 8 characters."));
 
        if (cmd.Country.Length != 2)
            return Result<RegisterDto>.Fail(
                AppError.Validation("country", "Country must be a 2-letter ISO code (e.g. CI, BJ, SN)."));
 
        // Email unique
        var existing = await uow.Merchants.GetByEmailAsync(cmd.Email.ToLowerInvariant(), ct);
        if (existing is not null)
            return Result<RegisterDto>.Fail(
                AppError.Conflict($"An account with email '{cmd.Email}' already exists."));
 
        // Créer le marchand + clés API
        var (merchant, liveKey, sandboxKey) =
            Domain.Merchants.Merchant.Create(
                cmd.BusinessName,
                cmd.Email.ToLowerInvariant(),
                cmd.Country.ToUpperInvariant());
 
        // Hash du mot de passe
        merchant.SetPassword(pwd.Hash(cmd.Password));
 
        // Générer JWT + Refresh Token immédiatement
        var accessToken  = tokens.GenerateAccessToken(
            merchant.Id, merchant.Email, merchant.Plan.ToString().ToLower());
        var refreshToken = tokens.GenerateRefreshToken();
        merchant.SetRefreshToken(refreshToken, DateTimeOffset.UtcNow.AddDays(7));
 
        await uow.Merchants.AddAsync(merchant, ct);
        await uow.SaveChangesAsync(ct);
 
        return Result<RegisterDto>.Ok(new RegisterDto(
            MerchantId:    merchant.Id.ToString(),
            LiveApiKey:    liveKey,
            SandboxApiKey: sandboxKey,
            AccessToken:   accessToken,
            RefreshToken:  refreshToken,
            ExpiresIn:     15 * 60,
            Warning:       "Store these API keys securely. They will not be shown again."));
    }
}