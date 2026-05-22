namespace AfriPay.Domain.Staff;

/// <summary>
/// Employé interne AfriPay.
/// Accède à l'Admin API avec un rôle et des permissions spécifiques.
/// Créé uniquement par un SuperAdmin.
/// </summary>
public sealed class StaffMember
{
    public Guid         Id                    { get; private set; }
    public string       Name                  { get; private set; } = default!;
    public string       Email                 { get; private set; } = default!;
    public string?      PasswordHash          { get; private set; }
    public StaffRole    Role                  { get; private set; }
    public StaffStatus  Status                { get; private set; }
    public string?      Department            { get; private set; }
 
    // Auth
    public string?        RefreshToken          { get; private set; }
    public DateTimeOffset? RefreshTokenExpiresAt { get; private set; }
    public DateTimeOffset? LastLoginAt           { get; private set; }
 
    public DateTimeOffset CreatedAt             { get; private set; }
    public DateTimeOffset UpdatedAt             { get; private set; }
    public Guid?          CreatedBy             { get; private set; } // StaffMember qui a créé
 
    private StaffMember() { }
 
    public static StaffMember Create(
        string    name,
        string    email,
        StaffRole role,
        Guid?     createdBy   = null,
        string?   department  = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new StaffDomainException("Name is required.");
 
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new StaffDomainException("Valid email is required.");
 
        return new StaffMember
        {
            Id         = Guid.NewGuid(),
            Name       = name,
            Email      = email.ToLowerInvariant(),
            Role       = role,
            Status     = StaffStatus.Active,
            Department = department,
            CreatedBy  = createdBy,
            CreatedAt  = DateTimeOffset.UtcNow,
            UpdatedAt  = DateTimeOffset.UtcNow,
        };
    }
 
    // Auth 
 
    public void SetPassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new StaffDomainException("Password hash cannot be empty.");
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
 
    // Gestion
 
    public void ChangeRole(StaffRole newRole, StaffMember actor)
    {
        if (actor.Role != StaffRole.SuperAdmin)
            throw new StaffDomainException("Only SuperAdmin can change staff roles.");
 
        Role      = newRole;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
 
    public void Suspend(StaffMember actor)
    {
        if (actor.Role != StaffRole.SuperAdmin)
            throw new StaffDomainException("Only SuperAdmin can suspend staff.");
 
        if (Status == StaffStatus.Suspended)
            throw new StaffDomainException("Staff member is already suspended.");
 
        Status = StaffStatus.Suspended;
        RevokeRefreshToken();
        UpdatedAt = DateTimeOffset.UtcNow;
    }
 
    public void Reactivate(StaffMember actor)
    {
        if (actor.Role != StaffRole.SuperAdmin)
            throw new StaffDomainException("Only SuperAdmin can reactivate staff.");
 
        if (Status == StaffStatus.Active)
            throw new StaffDomainException("Staff member is already active.");
 
        Status    = StaffStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
 
    // Permissions 
 
    public bool Can(string permission)
        => Status == StaffStatus.Active
        && StaffRolePermissions.Can(Role, permission);
 
    public IReadOnlySet<string> Permissions => StaffRolePermissions.For(Role);
}

public sealed class StaffDomainException(string message) : Exception(message);
