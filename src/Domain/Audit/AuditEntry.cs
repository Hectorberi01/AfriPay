namespace AfriPay.Domain.Audit;

/// <summary>
/// Entrée d'audit immuable.
/// Jamais UPDATE, jamais DELETE — garantie réglementaire.
/// </summary>
public sealed class AuditEntry
{
    public Guid           Id           { get; private set; }
    public Guid           MerchantId   { get; private set; }
    public AuditAction    Action       { get; private set; }
    public string         EntityType   { get; private set; } = default!;  // "Payment", "Merchant"…
    public string?        EntityId     { get; private set; }              // ID de la ressource concernée
    public string?        ActorId      { get; private set; }              // UserId ou "system"
    public string?        ActorType    { get; private set; }              // "merchant", "admin", "system"
    public string?        IpAddress    { get; private set; }
    public string?        UserAgent    { get; private set; }
    public string?        Metadata     { get; private set; }              // JSON des changements
    public DateTimeOffset OccurredAt   { get; private set; }
 
    private AuditEntry() { }
 
    public static AuditEntry Create(
        Guid          merchantId,
        AuditAction   action,
        string        entityType,
        string?       entityId    = null,
        string?       actorId     = null,
        string?       actorType   = "merchant",
        string?       ipAddress   = null,
        string?       userAgent   = null,
        string?       metadata    = null)
        => new()
        {
            Id          = Guid.NewGuid(),
            MerchantId  = merchantId,
            Action      = action,
            EntityType  = entityType,
            EntityId    = entityId,
            ActorId     = actorId,
            ActorType   = actorType,
            IpAddress   = ipAddress,
            UserAgent   = userAgent,
            Metadata    = metadata,
            OccurredAt  = DateTimeOffset.UtcNow,
        };
 
    public static AuditEntry System(
        Guid        merchantId,
        AuditAction action,
        string      entityType,
        string?     entityId = null,
        string?     metadata = null)
        => Create(merchantId, action, entityType, entityId,
                  actorId: "system", actorType: "system", metadata: metadata);
}