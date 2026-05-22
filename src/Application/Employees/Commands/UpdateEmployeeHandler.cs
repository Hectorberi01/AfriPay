using AfriPay.Application.Common.Errors;
using AfriPay.Application.Employees.Dto;
using AfriPay.Domain.Employees;
using AfriPay.Domain.Repositories;
namespace AfriPay.Application.Employees.Commands;

public sealed record UpdateEmployeeCommand(
    Guid   MerchantId,
    Guid   EmployeeId,
    string ActorRole,
    string? Name,
    string? Role);
 
public sealed class UpdateEmployeeHandler(IUnitOfWork uow)
{
    public async Task<Result<EmployeeDto>> HandleAsync(
        UpdateEmployeeCommand cmd, CancellationToken ct = default)
    {
        if (!EmployeePermissionHelper.CanManageTeam(cmd.ActorRole))
            return Result<EmployeeDto>.Fail(
                AppError.Forbidden("Only Owner or employees with team:manage can update team members."));
 
        var employee = await uow.Employees.GetByIdAsync(cmd.EmployeeId, ct);
        if (employee is null || employee.MerchantId != cmd.MerchantId)
            return Result<EmployeeDto>.Fail(
                AppError.NotFound("Employee", cmd.EmployeeId.ToString()));
 
        try
        {
            if (cmd.Name is not null) employee.UpdateName(cmd.Name);
            if (cmd.Role is not null && Enum.TryParse<EmployeeRole>(cmd.Role, ignoreCase: true, out var role))
                employee.ChangeRole(role);
        }
        catch (EmployeeDomainException ex)
        {
            return Result<EmployeeDto>.Fail(AppError.DomainRule(ex.Message));
        }
 
        uow.Employees.Update(employee);
        await uow.SaveChangesAsync(ct);
        return Result<EmployeeDto>.Ok(EmployeeDto.FromDomain(employee));
    }
}