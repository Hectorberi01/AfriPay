using AfriPay.Application.Common.Errors;
using AfriPay.Application.Employees.Dto;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Employees.Queries;

public sealed record GetEmployeeQuery(Guid MerchantId, Guid EmployeeId);
 
public sealed class GetEmployeeHandler(IUnitOfWork uow)
{
    public async Task<Result<EmployeeDto>> HandleAsync(
        GetEmployeeQuery query, CancellationToken ct = default)
    {
        var employee = await uow.Employees.GetByIdAsync(query.EmployeeId, ct);
        if (employee is null || employee.MerchantId != query.MerchantId)
            return Result<EmployeeDto>.Fail(
                AppError.NotFound("Employee", query.EmployeeId.ToString()));
        return Result<EmployeeDto>.Ok(EmployeeDto.FromDomain(employee));
    }
}