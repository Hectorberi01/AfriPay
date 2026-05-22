using AfriPay.Application.Auth.Interfaces;
using AfriPay.Application.Common.Errors;
using AfriPay.Application.Staff.Dtos;
using AfriPay.Domain.Repositories;
using AfriPay.Domain.Staff;

namespace AfriPay.Application.Staff.Commands;

public sealed record StaffLoginCommand(string Email, string Password);
 
public sealed class StaffLoginHandler(
    IUnitOfWork      uow,
    IPasswordService pwd,
    ITokenService    tokens)
{
    public async Task<Result<StaffLoginDto>> HandleAsync(
        StaffLoginCommand cmd, CancellationToken ct = default)
    {
        var staff = await uow.Staff.GetByEmailAsync(cmd.Email.ToLowerInvariant(), ct);
 
        if (staff is null || staff.PasswordHash is null)
            return Result<StaffLoginDto>.Fail(
                AppError.Unauthorized("Invalid credentials.","401"));
 
        if (!pwd.Verify(cmd.Password, staff.PasswordHash))
            return Result<StaffLoginDto>.Fail(
                AppError.Unauthorized("Invalid credentials.","401"));
 
        if (staff.Status == StaffStatus.Suspended)
            return Result<StaffLoginDto>.Fail(
                AppError.Unauthorized("Account suspended.","401"));
 
        // JWT avec rôle "staff:{role}" — distingue des marchands
        var accessToken  = tokens.GenerateAccessToken(
            staff.Id, staff.Email,
            staff.Role.ToString().ToLower(),
            role: $"staff:{staff.Role.ToString().ToLower()}");
 
        var refreshToken = tokens.GenerateRefreshToken();
        staff.SetRefreshToken(refreshToken, DateTimeOffset.UtcNow.AddDays(1)); // 1j seulement (sécurité)
        uow.Staff.Update(staff);
        await uow.SaveChangesAsync(ct);
 
        return Result<StaffLoginDto>.Ok(new StaffLoginDto(
            accessToken, refreshToken, 900,
            staff.Id.ToString(), staff.Name, staff.Email,
            staff.Role.ToString().ToLower(),
            staff.Permissions.ToList()));
    }
}