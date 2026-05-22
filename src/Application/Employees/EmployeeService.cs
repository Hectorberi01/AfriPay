using AfriPay.Application.Auth.Commands;
using AfriPay.Application.Common.Errors;
using AfriPay.Application.Employees.Commands;
using AfriPay.Application.Employees.Dto;
using AfriPay.Application.Employees.Queries;

namespace AfriPay.Application.Employees;

public interface IEmployeeService
{
    Task<Result<EmployeeDto>>                    CreateAsync(CreateEmployeeCommand cmd,      CancellationToken ct = default);
    Task<Result<EmployeeDto>>                    UpdateAsync(UpdateEmployeeCommand cmd,      CancellationToken ct = default);
    Task<Result<EmployeeDto>>                    SuspendAsync(SuspendEmployeeCommand cmd,    CancellationToken ct = default);
    Task<Result<EmployeeDto>>                    ReactivateAsync(ReactivateEmployeeCommand cmd, CancellationToken ct = default);
    Task<Result<EmployeeDto>>                    GetAsync(GetEmployeeQuery query,            CancellationToken ct = default);
    Task<Result<IReadOnlyList<EmployeeDto>>>     ListAsync(ListEmployeesQuery query,         CancellationToken ct = default);
    Task<Result<EmployeeLoginDto>>               LoginAsync(EmployeeLoginCommand cmd,        CancellationToken ct = default);
}
 
public sealed class EmployeeService(
    CreateEmployeeHandler    createHandler,
    UpdateEmployeeHandler    updateHandler,
    SuspendEmployeeHandler   suspendHandler,
    ReactivateEmployeeHandler reactivateHandler,
    GetEmployeeHandler       getHandler,
    ListEmployeesHandler     listHandler,
    EmployeeLoginHandler     loginHandler) : IEmployeeService
{
    public Task<Result<EmployeeDto>>                CreateAsync(CreateEmployeeCommand cmd, CancellationToken ct = default)          => createHandler.HandleAsync(cmd, ct);
    public Task<Result<EmployeeDto>>                UpdateAsync(UpdateEmployeeCommand cmd, CancellationToken ct = default)          => updateHandler.HandleAsync(cmd, ct);
    public Task<Result<EmployeeDto>>                SuspendAsync(SuspendEmployeeCommand cmd, CancellationToken ct = default)        => suspendHandler.HandleAsync(cmd, ct);
    public Task<Result<EmployeeDto>>                ReactivateAsync(ReactivateEmployeeCommand cmd, CancellationToken ct = default)  => reactivateHandler.HandleAsync(cmd, ct);
    public Task<Result<EmployeeDto>>                GetAsync(GetEmployeeQuery query, CancellationToken ct = default)                => getHandler.HandleAsync(query, ct);
    public Task<Result<IReadOnlyList<EmployeeDto>>> ListAsync(ListEmployeesQuery query, CancellationToken ct = default)            => listHandler.HandleAsync(query, ct);
    public Task<Result<EmployeeLoginDto>>           LoginAsync(EmployeeLoginCommand cmd, CancellationToken ct = default)           => loginHandler.HandleAsync(cmd, ct);
}
 