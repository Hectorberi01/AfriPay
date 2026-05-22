namespace AfriPay.Domain.Kyb;

public sealed class KybDocument
{
    public Guid           Id           { get; private set; }
    public Guid           KybId        { get; private set; }
    public DocumentType   Type         { get; private set; }
    public string         FileName     { get; private set; } = default!;
    public string         StorageKey   { get; private set; } = default!;  // S3/GCS key
    public string         ContentType  { get; private set; } = default!;
    public long           FileSizeBytes{ get; private set; }
    public DocumentStatus Status       { get; private set; }
    public string?        ReviewNote   { get; private set; }
    public DateTimeOffset UploadedAt   { get; private set; }
    public DateTimeOffset? ReviewedAt  { get; private set; }
 
    private KybDocument() { }
 
    public static KybDocument Create(
        Guid         kybId,
        DocumentType type,
        string       fileName,
        string       storageKey,
        string       contentType,
        long         fileSizeBytes)
    {
        if (fileSizeBytes > 10 * 1024 * 1024) // 10 MB max
            throw new KybDomainException($"Document too large: {fileSizeBytes} bytes (max 10MB).");
 
        var allowed = new[] { "application/pdf", "image/jpeg", "image/png" };
        if (!allowed.Contains(contentType))
            throw new KybDomainException($"Unsupported file type: {contentType}.");
 
        return new KybDocument
        {
            Id            = Guid.NewGuid(),
            KybId         = kybId,
            Type          = type,
            FileName      = fileName,
            StorageKey    = storageKey,
            ContentType   = contentType,
            FileSizeBytes = fileSizeBytes,
            Status        = DocumentStatus.Pending,
            UploadedAt    = DateTimeOffset.UtcNow,
        };
    }
 
    public void Accept(string? note = null)
    {
        Status     = DocumentStatus.Accepted;
        ReviewNote = note;
        ReviewedAt = DateTimeOffset.UtcNow;
    }
 
    public void Reject(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new KybDomainException("Rejection reason is required.");
 
        Status     = DocumentStatus.Rejected;
        ReviewNote = reason;
        ReviewedAt = DateTimeOffset.UtcNow;
    }
}