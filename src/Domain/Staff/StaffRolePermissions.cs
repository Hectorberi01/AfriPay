namespace AfriPay.Domain.Staff;

public static class StaffRolePermissions
{
    public static readonly IReadOnlySet<string> SuperAdmin = new HashSet<string>
    {
        "merchants:read",    "merchants:write",   "merchants:suspend",
        "kyb:read",          "kyb:approve",       "kyb:reject",
        "disputes:read",     "disputes:resolve",
        "payments:read",
        "payouts:read",      "payouts:manage",
        "analytics:read",
        "fees:read",         "fees:write",
        "staff:read",        "staff:manage",
        "providers:read",    "providers:write",
        "audit:read",
        "config:read",       "config:write",
    };
 
    public static readonly IReadOnlySet<string> Admin = new HashSet<string>
    {
        "merchants:read",    "merchants:suspend",
        "kyb:read",          "kyb:approve",       "kyb:reject",
        "disputes:read",     "disputes:resolve",
        "payments:read",
        "analytics:read",
        "audit:read",
        "fees:read",
    };
 
    public static readonly IReadOnlySet<string> Finance = new HashSet<string>
    {
        "merchants:read",
        "payments:read",
        "payouts:read",      "payouts:manage",
        "analytics:read",
        "fees:read",         "fees:write",
        "audit:read",
    };
 
    public static readonly IReadOnlySet<string> Support = new HashSet<string>
    {
        "merchants:read",
        "payments:read",
        "disputes:read",
        "kyb:read",
        "audit:read",
    };
 
    public static readonly IReadOnlySet<string> Developer = new HashSet<string>
    {
        "merchants:read",
        "payments:read",
        "providers:read",    "providers:write",
        "config:read",       "config:write",
        "audit:read",
    };
 
    public static IReadOnlySet<string> For(StaffRole role) => role switch
    {
        StaffRole.SuperAdmin => SuperAdmin,
        StaffRole.Admin      => Admin,
        StaffRole.Finance    => Finance,
        StaffRole.Support    => Support,
        StaffRole.Developer  => Developer,
        _                    => Support,
    };
 
    public static bool Can(StaffRole role, string permission)
        => For(role).Contains(permission);
}