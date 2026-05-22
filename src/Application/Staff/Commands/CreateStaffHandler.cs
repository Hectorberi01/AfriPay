using AfriPay.Application.Auth.Interfaces;
using AfriPay.Application.Common.Errors;
using AfriPay.Application.Staff.Dtos;
using AfriPay.Domain.Repositories;
using AfriPay.Domain.Staff;

namespace AfriPay.Application.Staff.Commands;

public sealed record CreateStaffCommand(
    Guid    ActorId,     // SuperAdmin qui crée
    string  Name,
    string  Email,
    string  Password,
    string  Role,
    string? Department);
 
public sealed class CreateStaffHandler(
    IUnitOfWork      uow,
    IPasswordService pwd)
{
    public async Task<Result<StaffDto>> HandleAsync(
        CreateStaffCommand cmd, CancellationToken ct = default)
    {
        // Vérifier que l'acteur est SuperAdmin
        var actor = await uow.Staff.GetByIdAsync(cmd.ActorId, ct);
        if (actor is null || actor.Role != StaffRole.SuperAdmin)
            return Result<StaffDto>.Fail(
                AppError.Forbidden("Only SuperAdmin can create staff members."));
 
        // Email unique
        var existing = await uow.Staff.GetByEmailAsync(cmd.Email, ct);
        if (existing is not null)
            return Result<StaffDto>.Fail(
                AppError.Conflict($"Staff member with email '{cmd.Email}' already exists."));
 
        if (cmd.Password.Length < 10)
            return Result<StaffDto>.Fail(
                AppError.Validation("password",
                    "Staff passwords must be at least 10 characters."));
 
        if (!Enum.TryParse<StaffRole>(cmd.Role, ignoreCase: true, out var role))
            return Result<StaffDto>.Fail(
                AppError.Validation("role",
                    $"Valid roles: {string.Join(", ", Enum.GetNames<StaffRole>())}."));
 
        StaffMember staff;
        try
        {
            staff = StaffMember.Create(cmd.Name, cmd.Email, role, cmd.ActorId, cmd.Department);
        }
        catch (StaffDomainException ex)
        {
            return Result<StaffDto>.Fail(AppError.DomainRule(ex.Message));
        }
 
        staff.SetPassword(pwd.Hash(cmd.Password));
        await uow.Staff.AddAsync(staff, ct);
        await uow.SaveChangesAsync(ct);
 
        return Result<StaffDto>.Ok(StaffDto.FromDomain(staff));
    }
}