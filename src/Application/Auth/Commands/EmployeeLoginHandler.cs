using AfriPay.Application.Auth.Interfaces;
using AfriPay.Application.Common.Errors;
using AfriPay.Domain.Employees;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Auth.Commands;

public sealed record EmployeeLoginCommand(string Email, string Password);
 
public sealed record EmployeeLoginDto(
    string AccessToken,
    string RefreshToken,
    int    ExpiresIn,
    string EmployeeId,
    string MerchantId,
    string Name,
    string Email,
    string Role,
    IReadOnlyList<string> Permissions);
 
public sealed class EmployeeLoginHandler(
    IUnitOfWork      uow,
    IPasswordService pwd,
    ITokenService    tokens)
{
    public async Task<Result<EmployeeLoginDto>> HandleAsync(
        EmployeeLoginCommand cmd, CancellationToken ct = default)
    {
        var employee = await uow.Employees.GetByEmailGlobalAsync(cmd.Email.ToLowerInvariant(), ct);
 
        if (employee is null || employee.PasswordHash is null)
            return Result<EmployeeLoginDto>.Fail(
                AppError.Unauthorized("Invalid credentials.","401"));
 
        if (!pwd.Verify(cmd.Password, employee.PasswordHash))
            return Result<EmployeeLoginDto>.Fail(
                AppError.Unauthorized("Invalid credentials.","401"));
 
        if (employee.Status == EmployeeStatus.Suspended)
            return Result<EmployeeLoginDto>.Fail(
                AppError.Unauthorized("Account suspended. Contact your administrator.","401"));
 
        var accessToken  = tokens.GenerateAccessToken(
            employee.Id,
            employee.Email,
            employee.Role.ToString().ToLower(),
            role: $"employee:{employee.Role.ToString().ToLower()}");
 
        var refreshToken = tokens.GenerateRefreshToken();
        employee.SetRefreshToken(refreshToken, DateTimeOffset.UtcNow.AddDays(7));
        uow.Employees.Update(employee);
        await uow.SaveChangesAsync(ct);
 
        return Result<EmployeeLoginDto>.Ok(new EmployeeLoginDto(
            AccessToken:  accessToken,
            RefreshToken: refreshToken,
            ExpiresIn:    900,
            EmployeeId:   employee.Id.ToString(),
            MerchantId:   employee.MerchantId.ToString(),
            Name:         employee.Name,
            Email:        employee.Email,
            Role:         employee.Role.ToString().ToLower(),
            Permissions:  employee.Permissions.ToList()));
    }
}