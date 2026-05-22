using AfriPay.Application.Auth.Interfaces;
using AfriPay.Application.Common.Errors;
using AfriPay.Application.Employees.Dto;
using AfriPay.Domain.Employees;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Employees.Commands;

public sealed record CreateEmployeeCommand(
    Guid   MerchantId,
    Guid   ActorId,        // Celui qui crée (owner ou team:manage)
    string ActorRole,      // Rôle de l'acteur pour vérifier permission
    string Name,
    string Email,
    string Password,
    string Role);
 
public sealed class CreateEmployeeHandler(
    IUnitOfWork      uow,
    IPasswordService pwd)
{
    public async Task<Result<EmployeeDto>> HandleAsync(
        CreateEmployeeCommand cmd, CancellationToken ct = default)
    {
        // Vérifier permission
        if (!CanManageTeamRole(cmd.ActorRole))
            return Result<EmployeeDto>.Fail(
                AppError.Forbidden("Only Owner or employees with team:manage can add team members."));
 
        // Email unique dans le scope du marchand
        var existing = await uow.Employees.GetByEmailAsync(cmd.MerchantId, cmd.Email, ct);
        if (existing is not null)
            return Result<EmployeeDto>.Fail(
                AppError.Conflict($"An employee with email '{cmd.Email}' already exists."));
 
        if (cmd.Password.Length < 8)
            return Result<EmployeeDto>.Fail(
                AppError.Validation("password", "Password must be at least 8 characters."));
 
        if (!Enum.TryParse<EmployeeRole>(cmd.Role, ignoreCase: true, out var role))
            return Result<EmployeeDto>.Fail(
                AppError.Validation("role",
                    $"Valid roles: {string.Join(", ", Enum.GetNames<EmployeeRole>().Where(r => r != "Owner"))}."));
 
        Employee employee;
        try { employee = Employee.Create(cmd.MerchantId, cmd.Name, cmd.Email, role); }
        catch (EmployeeDomainException ex)
        {
            return Result<EmployeeDto>.Fail(AppError.DomainRule(ex.Message));
        }
 
        employee.SetPassword(pwd.Hash(cmd.Password));
        await uow.Employees.AddAsync(employee, ct);
        await uow.SaveChangesAsync(ct);
 
        return Result<EmployeeDto>.Ok(EmployeeDto.FromDomain(employee));
    }
    
    private static bool CanManageTeamRole(string role)
        => role.Equals("owner", StringComparison.OrdinalIgnoreCase)
           || (Enum.TryParse<EmployeeRole>(role, ignoreCase: true, out var r)
               && RolePermissions.Can(r, "team:manage"));
}