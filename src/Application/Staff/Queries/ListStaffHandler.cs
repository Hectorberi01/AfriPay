using AfriPay.Application.Common.Errors;
using AfriPay.Application.Staff.Dtos;
using AfriPay.Domain.Repositories;
using AfriPay.Domain.Staff;

namespace AfriPay.Application.Staff.Queries;

public sealed record ListStaffQuery(string? Role = null, string? Status = null);
 
public sealed class ListStaffHandler(IUnitOfWork uow)
{
    public async Task<Result<IReadOnlyList<StaffDto>>> HandleAsync(
        ListStaffQuery query, CancellationToken ct = default)
    {
        StaffRole?   role   = Enum.TryParse<StaffRole>(query.Role, ignoreCase: true, out var r)   ? r : null;
        StaffStatus? status = Enum.TryParse<StaffStatus>(query.Status, ignoreCase: true, out var s) ? s : null;
 
        var staff = await uow.Staff.ListAsync(role, status, ct);
        var dtos  = (IReadOnlyList<StaffDto>)staff.Select(StaffDto.FromDomain).ToList();
        return Result<IReadOnlyList<StaffDto>>.Ok(dtos);
    }
}