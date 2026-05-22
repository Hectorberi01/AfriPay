using AfriPay.Domain.Employees;

namespace AfriPay.Application.Employees;

public static class EmployeePermissionHelper
{
    public static bool CanManageTeam(string role)
        => role.Equals("owner", StringComparison.OrdinalIgnoreCase)
           || (Enum.TryParse<EmployeeRole>(role, ignoreCase: true, out var r)
               && RolePermissions.Can(r, "team:manage"));
}