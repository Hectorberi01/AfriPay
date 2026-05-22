using AfriPay.Application.Common.Errors;
using AfriPay.Application.Staff.Dtos;
using AfriPay.Domain.Repositories;
using AfriPay.Domain.Staff;

namespace AfriPay.Application.Staff.Commands;

public sealed record ChangeStaffRoleCommand(Guid ActorId, Guid StaffId, string NewRole);
 
public sealed class ChangeStaffRoleHandler(IUnitOfWork uow)
{
    public async Task<Result<StaffDto>> HandleAsync(
        ChangeStaffRoleCommand cmd, CancellationToken ct = default)
    {
        var actor  = await uow.Staff.GetByIdAsync(cmd.ActorId, ct);
        var target = await uow.Staff.GetByIdAsync(cmd.StaffId, ct);
 
        if (actor is null || target is null)
            return Result<StaffDto>.Fail(AppError.NotFound("StaffMember", cmd.StaffId.ToString()));
 
        if (!Enum.TryParse<StaffRole>(cmd.NewRole, ignoreCase: true, out var role))
            return Result<StaffDto>.Fail(AppError.Validation("role", "Invalid role."));
 
        try { target.ChangeRole(role, actor); }
        catch (StaffDomainException ex)
        {
            return Result<StaffDto>.Fail(AppError.DomainRule(ex.Message));
        }
 
        uow.Staff.Update(target);
        await uow.SaveChangesAsync(ct);
        return Result<StaffDto>.Ok(StaffDto.FromDomain(target));
    }
}