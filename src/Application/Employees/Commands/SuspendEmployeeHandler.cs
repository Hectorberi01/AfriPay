using AfriPay.Application.Common.Errors;
using AfriPay.Application.Employees.Dto;
using AfriPay.Domain.Employees;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Employees.Commands;

public sealed record SuspendEmployeeCommand(Guid MerchantId, Guid EmployeeId, string ActorRole);
 
public sealed class SuspendEmployeeHandler(IUnitOfWork uow)
{
    public async Task<Result<EmployeeDto>> HandleAsync(
        SuspendEmployeeCommand cmd, CancellationToken ct = default)
    {
        if (!EmployeePermissionHelper.CanManageTeam(cmd.ActorRole))
            return Result<EmployeeDto>.Fail(AppError.Forbidden("Insufficient permissions."));
 
        var employee = await uow.Employees.GetByIdAsync(cmd.EmployeeId, ct);
        if (employee is null || employee.MerchantId != cmd.MerchantId)
            return Result<EmployeeDto>.Fail(
                AppError.NotFound("Employee", cmd.EmployeeId.ToString()));
 
        try { employee.Suspend(); }
        catch (EmployeeDomainException ex)
        {
            return Result<EmployeeDto>.Fail(AppError.DomainRule(ex.Message));
        }
 
        uow.Employees.Update(employee);
        await uow.SaveChangesAsync(ct);
        return Result<EmployeeDto>.Ok(EmployeeDto.FromDomain(employee));
    }
}