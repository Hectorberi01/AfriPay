namespace AfriPay.Domain.Disputes;

public sealed class DisputeEvidence
{
    public Guid           Id          { get; private set; }
    public Guid           DisputeId   { get; private set; }
    public EvidenceType   Type        { get; private set; }
    public string         FileName    { get; private set; } = default!;
    public string         StorageKey  { get; private set; } = default!;
    public string?        Description { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }
 
    private DisputeEvidence() { }
 
    public static DisputeEvidence Create(
        Guid disputeId, EvidenceType type,
        string fileName, string storageKey, string? description = null)
        => new()
        {
            Id = Guid.NewGuid(), DisputeId = disputeId, Type = type,
            FileName = fileName, StorageKey = storageKey,
            Description = description, SubmittedAt = DateTimeOffset.UtcNow,
        };
}