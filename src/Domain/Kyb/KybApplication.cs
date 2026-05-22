namespace AfriPay.Domain.Kyb;

/// <summary>
/// Dossier KYB d'un marchand.
///
/// Machine à états :
///   NotStarted → Draft → Submitted → UnderReview
///     → Approved  (accès live débloqué)
///     → Rejected  (peut être résoumis après 30j)
///     → AdditionalInfoRequired → Submitted
/// </summary>
public sealed class KybApplication
{
    public Guid       Id           { get; private set; }
    public Guid       MerchantId   { get; private set; }
    public KybStatus  Status       { get; private set; }
 
    // Informations légales
    public string     LegalName    { get; private set; } = default!;
    public string     Country      { get; private set; } = default!;
    public string?    RegistrationNumber { get; private set; }
    public string?    TaxId        { get; private set; }
    public string?    BusinessType { get; private set; }  // "sarl", "sa", "auto-entrepreneur"
    public string?    Website      { get; private set; }
 
    // Contact légal
    public string?    LegalRepresentativeName  { get; private set; }
    public string?    LegalRepresentativeEmail { get; private set; }
 
    // Review
    public string?    ReviewedBy   { get; private set; }  // Email admin
    public string?    ReviewNote   { get; private set; }
    public DateTimeOffset? ReviewedAt    { get; private set; }
    public DateTimeOffset? ApprovedAt    { get; private set; }
    public DateTimeOffset? ExpiresAt     { get; private set; }
 
    public DateTimeOffset SubmittedAt   { get; private set; }
    public DateTimeOffset CreatedAt     { get; private set; }
    public DateTimeOffset UpdatedAt     { get; private set; }
 
    private readonly List<KybDocument> _documents = [];
    public IReadOnlyList<KybDocument> Documents => _documents.AsReadOnly();
 
    private KybApplication() { }
 
    public static KybApplication Create(Guid merchantId, string country)
        => new()
        {
            Id         = Guid.NewGuid(),
            MerchantId = merchantId,
            Country    = country.ToUpperInvariant(),
            Status     = KybStatus.NotStarted,
            CreatedAt  = DateTimeOffset.UtcNow,
            UpdatedAt  = DateTimeOffset.UtcNow,
            SubmittedAt = DateTimeOffset.MinValue,
        };
 
    // ── Remplissage du dossier ─────────────────────────────────
 
    public void SetBusinessInfo(
        string  legalName,
        string? registrationNumber,
        string? taxId,
        string? businessType,
        string? website)
    {
        EnsureEditable();
        LegalName            = legalName;
        RegistrationNumber   = registrationNumber;
        TaxId                = taxId;
        BusinessType         = businessType;
        Website              = website;
        Status               = KybStatus.Draft;
        UpdatedAt            = DateTimeOffset.UtcNow;
    }
 
    public void SetLegalRepresentative(string name, string email)
    {
        EnsureEditable();
        LegalRepresentativeName  = name;
        LegalRepresentativeEmail = email;
        UpdatedAt                = DateTimeOffset.UtcNow;
    }
 
    public KybDocument AddDocument(
        DocumentType type,
        string       fileName,
        string       storageKey,
        string       contentType,
        long         fileSizeBytes)
    {
        EnsureEditable();
 
        var doc = KybDocument.Create(Id, type, fileName, storageKey, contentType, fileSizeBytes);
        _documents.Add(doc);
        UpdatedAt = DateTimeOffset.UtcNow;
        return doc;
    }
 
    // Machine à états
 
    public void Submit()
    {
        if (Status is not (KybStatus.Draft or KybStatus.AdditionalInfoRequired))
            throw new KybDomainException(
                $"Cannot submit KYB in status '{Status}'.");
 
        var required = new[]
        {
            DocumentType.BusinessRegistration,
            DocumentType.OwnerIdentity
        };
        
        var missing  = required
            .Where(t => !_documents.Any(d => d.Type == t))
            .ToList();
 
        if (missing.Any())
            throw new KybDomainException(
                $"Missing required documents: {string.Join(", ", missing)}.");
 
        if (string.IsNullOrWhiteSpace(LegalName))
            throw new KybDomainException("Legal name is required before submitting.");
 
        Status      = KybStatus.Submitted;
        SubmittedAt = DateTimeOffset.UtcNow;
        UpdatedAt   = DateTimeOffset.UtcNow;
    }
 
    public void StartReview(string reviewerEmail)
    {
        if (Status != KybStatus.Submitted)
            throw new KybDomainException("Only submitted applications can be reviewed.");
 
        Status     = KybStatus.UnderReview;
        ReviewedBy = reviewerEmail;
        UpdatedAt  = DateTimeOffset.UtcNow;
    }
 
    public void Approve(string reviewerEmail, string? note = null)
    {
        if (Status is not (KybStatus.UnderReview or KybStatus.Submitted))
            throw new KybDomainException("Only under-review applications can be approved.");
 
        Status     = KybStatus.Approved;
        ReviewedBy = reviewerEmail;
        ReviewNote = note;
        ReviewedAt = DateTimeOffset.UtcNow;
        ApprovedAt = DateTimeOffset.UtcNow;
        // KYB valide 1 an
        ExpiresAt  = DateTimeOffset.UtcNow.AddYears(1);
        UpdatedAt  = DateTimeOffset.UtcNow;
    }
 
    public void Reject(string reviewerEmail, string reason)
    {
        if (Status is not (KybStatus.UnderReview or KybStatus.Submitted))
            throw new KybDomainException("Only under-review applications can be rejected.");
 
        if (string.IsNullOrWhiteSpace(reason))
            throw new KybDomainException("Rejection reason is required.");
 
        Status     = KybStatus.Rejected;
        ReviewedBy = reviewerEmail;
        ReviewNote = reason;
        ReviewedAt = DateTimeOffset.UtcNow;
        UpdatedAt  = DateTimeOffset.UtcNow;
    }
 
    public void RequestAdditionalInfo(string reviewerEmail, string request)
    {
        if (Status != KybStatus.UnderReview)
            throw new KybDomainException("Can only request info during review.");
 
        Status     = KybStatus.AdditionalInfoRequired;
        ReviewedBy = reviewerEmail;
        ReviewNote = request;
        UpdatedAt  = DateTimeOffset.UtcNow;
    }
 
    // Computed
 
    public bool IsApproved   => Status == KybStatus.Approved && ExpiresAt > DateTimeOffset.UtcNow;
    public bool LiveAccessAllowed => IsApproved;
 
    // Guard 
 
    private void EnsureEditable()
    {
        if (Status is KybStatus.Submitted or KybStatus.UnderReview or KybStatus.Approved)
            throw new KybDomainException(
                $"KYB application in status '{Status}' cannot be modified.");
    }
}

public sealed class KybDomainException(string message) : Exception(message);