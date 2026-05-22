using AfriPay.Application.Common.Errors;
using AfriPay.Application.Staff.Commands;
using AfriPay.Application.Staff.Dtos;
using AfriPay.Application.Staff.Queries;

namespace AfriPay.Application.Staff;

public interface IStaffService
{
    Task<Result<StaffLoginDto>>              LoginAsync(StaffLoginCommand cmd,          CancellationToken ct = default);
    Task<Result<StaffDto>>                   CreateAsync(CreateStaffCommand cmd,        CancellationToken ct = default);
    Task<Result<StaffDto>>                   ChangeRoleAsync(ChangeStaffRoleCommand cmd, CancellationToken ct = default);
    Task<Result<StaffDto>>                   SuspendAsync(SuspendStaffCommand cmd,      CancellationToken ct = default);
    Task<Result<IReadOnlyList<StaffDto>>>    ListAsync(ListStaffQuery query,            CancellationToken ct = default);
}
 
public sealed class StaffService(
    StaffLoginHandler      loginHandler,
    CreateStaffHandler     createHandler,
    ChangeStaffRoleHandler changeRoleHandler,
    SuspendStaffHandler    suspendHandler,
    ListStaffHandler       listHandler) : IStaffService
{
    public Task<Result<StaffLoginDto>>           LoginAsync(StaffLoginCommand cmd, CancellationToken ct = default)           => loginHandler.HandleAsync(cmd, ct);
    public Task<Result<StaffDto>>                CreateAsync(CreateStaffCommand cmd, CancellationToken ct = default)         => createHandler.HandleAsync(cmd, ct);
    public Task<Result<StaffDto>>                ChangeRoleAsync(ChangeStaffRoleCommand cmd, CancellationToken ct = default) => changeRoleHandler.HandleAsync(cmd, ct);
    public Task<Result<StaffDto>>                SuspendAsync(SuspendStaffCommand cmd, CancellationToken ct = default)       => suspendHandler.HandleAsync(cmd, ct);
    public Task<Result<IReadOnlyList<StaffDto>>> ListAsync(ListStaffQuery query, CancellationToken ct = default)             => listHandler.HandleAsync(query, ct);
}