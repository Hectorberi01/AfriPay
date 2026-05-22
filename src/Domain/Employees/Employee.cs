namespace AfriPay.Domain.Employees;

public sealed class Employee
{
    public Guid           Id                    { get; private set; }
    public Guid           MerchantId            { get; private set; }
    public string         Name                  { get; private set; } = default!;
    public string         Email                 { get; private set; } = default!;
    public string?        PasswordHash          { get; private set; }
    public EmployeeRole   Role                  { get; private set; }
    public EmployeeStatus Status                { get; private set; }
 
    // Auth
    public string?        RefreshToken          { get; private set; }
    public DateTimeOffset? RefreshTokenExpiresAt { get; private set; }
 
    public DateTimeOffset CreatedAt             { get; private set; }
    public DateTimeOffset UpdatedAt             { get; private set; }
    public DateTimeOffset? LastLoginAt          { get; private set; }
    
    private Employee() { }
 
    public static Employee Create(
        Guid         merchantId,
        string       name,
        string       email,
        EmployeeRole role)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new EmployeeDomainException("Name is required.");
 
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new EmployeeDomainException("Valid email is required.");
 
        if (role == EmployeeRole.Owner)
            throw new EmployeeDomainException(
                "Owner role cannot be assigned to employees. " +
                "The owner is the merchant who created the account.");
 
        return new Employee
        {
            Id         = Guid.NewGuid(),
            MerchantId = merchantId,
            Name       = name,
            Email      = email.ToLowerInvariant(),
            Role       = role,
            Status     = EmployeeStatus.Active,
            CreatedAt  = DateTimeOffset.UtcNow,
            UpdatedAt  = DateTimeOffset.UtcNow,
        };
    }
    
    public void SetPassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new EmployeeDomainException("Password hash cannot be empty.");
        PasswordHash = passwordHash;
        UpdatedAt    = DateTimeOffset.UtcNow;
    }
 
    public void SetRefreshToken(string token, DateTimeOffset expiresAt)
    {
        RefreshToken          = token;
        RefreshTokenExpiresAt = expiresAt;
        LastLoginAt           = DateTimeOffset.UtcNow;
        UpdatedAt             = DateTimeOffset.UtcNow;
    }
 
    public void RevokeRefreshToken()
    {
        RefreshToken          = null;
        RefreshTokenExpiresAt = null;
        UpdatedAt             = DateTimeOffset.UtcNow;
    }
 
    public bool HasValidRefreshToken(string token)
        => RefreshToken == token
           && RefreshTokenExpiresAt.HasValue
           && RefreshTokenExpiresAt > DateTimeOffset.UtcNow;
    
    public void ChangeRole(EmployeeRole newRole)
    {
        if (newRole == EmployeeRole.Owner)
            throw new EmployeeDomainException("Owner role cannot be assigned.");
 
        Role      = newRole;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
 
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new EmployeeDomainException("Name is required.");
        Name      = name;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
 
    public void Suspend()
    {
        if (Status == EmployeeStatus.Suspended)
            throw new EmployeeDomainException("Employee is already suspended.");
        Status    = EmployeeStatus.Suspended;
        RevokeRefreshToken();
    }
 
    public void Reactivate()
    {
        if (Status == EmployeeStatus.Active)
            throw new EmployeeDomainException("Employee is already active.");
        Status    = EmployeeStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
    
    public bool Can(string permission)
        => Status == EmployeeStatus.Active
           && RolePermissions.Can(Role, permission);
 
    public IReadOnlySet<string> Permissions
        => RolePermissions.For(Role);
}

public sealed class EmployeeDomainException(string message) : Exception(message);
