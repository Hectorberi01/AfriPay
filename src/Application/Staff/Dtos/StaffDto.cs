using AfriPay.Domain.Staff;

namespace AfriPay.Application.Staff.Dtos;

public sealed record StaffDto(
    string          StaffId,
    string          Name,
    string          Email,
    string          Role,
    string          Status,
    string?         Department,
    IReadOnlyList<string> Permissions,
    DateTimeOffset  CreatedAt,
    DateTimeOffset? LastLoginAt)
{
    public static StaffDto FromDomain(StaffMember s) => new(
        s.Id.ToString(), s.Name, s.Email,
        s.Role.ToString().ToLower(),
        s.Status.ToString().ToLower(),
        s.Department,
        s.Permissions.ToList(),
        s.CreatedAt, s.LastLoginAt);
}
 
public sealed record StaffLoginDto(
    string AccessToken,
    string RefreshToken,
    int    ExpiresIn,
    string StaffId,
    string Name,
    string Email,
    string Role,
    IReadOnlyList<string> Permissions);