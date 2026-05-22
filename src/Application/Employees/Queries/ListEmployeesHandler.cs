using AfriPay.Application.Common.Errors;
using AfriPay.Application.Employees.Dto;
using AfriPay.Domain.Employees;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Employees.Queries;

public sealed record ListEmployeesQuery(Guid MerchantId, string? Role = null);
 
public sealed class ListEmployeesHandler(IUnitOfWork uow)
{
    public async Task<Result<IReadOnlyList<EmployeeDto>>> HandleAsync(
        ListEmployeesQuery query, CancellationToken ct = default)
    {
        EmployeeRole? role = query.Role is not null
                             && Enum.TryParse<EmployeeRole>(query.Role, ignoreCase: true, out var r)
            ? r : null;
 
        var employees = await uow.Employees.GetByMerchantAsync(query.MerchantId, role, ct);
        var dtos      = (IReadOnlyList<EmployeeDto>)employees
            .Select(EmployeeDto.FromDomain).ToList();
 
        return Result<IReadOnlyList<EmployeeDto>>.Ok(dtos);
    }
}