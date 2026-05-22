using AfriPay.Domain.Employees;

namespace AfriPay.Application.Employees.Dto;

public sealed record EmployeeDto(
    string          EmployeeId,
    string          MerchantId,
    string          Name,
    string          Email,
    string          Role,
    string          Status,
    IReadOnlyList<string> Permissions,
    DateTimeOffset  CreatedAt,
    DateTimeOffset? LastLoginAt)
{
    public static EmployeeDto FromDomain(Employee e) => new(
        e.Id.ToString(),
        e.MerchantId.ToString(),
        e.Name,
        e.Email,
        e.Role.ToString().ToLower(),
        e.Status.ToString().ToLower(),
        e.Permissions.ToList(),
        e.CreatedAt,
        e.LastLoginAt);
}
