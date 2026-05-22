namespace AfriPay.Domain.Employees;

public static class RolePermissions
{
    public static readonly IReadOnlySet<string> Owner = new HashSet<string>
    {
        "payments:read",   "payments:write",
        "refunds:read",    "refunds:write",
        "payouts:read",    "payouts:write",
        "analytics:read",
        "kyb:read",        "kyb:write",
        "webhooks:read",   "webhooks:write",
        "api_keys:read",   "api_keys:write",
        "team:read",       "team:manage",
        "disputes:read",
        "subscriptions:read", "subscriptions:write",
        "balance:read",
    };
 
    public static readonly IReadOnlySet<string> Developer = new HashSet<string>
    {
        "payments:read",
        "refunds:read",
        "webhooks:read",   "webhooks:write",
        "api_keys:read",
        "analytics:read",
        "balance:read",
    };
 
    public static readonly IReadOnlySet<string> Finance = new HashSet<string>
    {
        "payments:read",   "payments:write",
        "refunds:read",    "refunds:write",
        "payouts:read",    "payouts:write",
        "analytics:read",
        "balance:read",
        "disputes:read",
        "subscriptions:read",
    };
 
    public static readonly IReadOnlySet<string> Support = new HashSet<string>
    {
        "payments:read",
        "refunds:read",
        "disputes:read",
        "subscriptions:read",
        "balance:read",
    };
 
    public static readonly IReadOnlySet<string> Viewer = new HashSet<string>
    {
        "payments:read",
        "analytics:read",
        "balance:read",
    };
 
    public static IReadOnlySet<string> For(EmployeeRole role) => role switch
    {
        EmployeeRole.Owner     => Owner,
        EmployeeRole.Developer => Developer,
        EmployeeRole.Finance   => Finance,
        EmployeeRole.Support   => Support,
        EmployeeRole.Viewer    => Viewer,
        _                      => Viewer,
    };
 
    public static bool Can(EmployeeRole role, string permission)
        => For(role).Contains(permission);
}