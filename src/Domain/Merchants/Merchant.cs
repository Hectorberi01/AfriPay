using AfriPay.Domain.Merchants.ValueObjects;

namespace AfriPay.Domain.Merchants;

/// <summary>
/// Agrégat racine représentant un marchand inscrit sur la plateforme AfriPay.
/// Encapsule le cycle de vie KYB, les clés d'API, la configuration webhook et les limites liées au plan tarifaire.
/// </summary>
public sealed class Merchant
{
    /// <summary>Identifiant unique du marchand.</summary>
    public Guid   Id              { get; private set; }

    /// <summary>Raison sociale ou nom commercial du marchand.</summary>
    public string BusinessName    { get; private set; } = default!;

    /// <summary>Adresse e-mail de contact principale.</summary>
    public string Email           { get; private set; } = default!;

    /// <summary>Code pays ISO 3166-1 alpha-2 du marchand (ex. <c>SN</c>, <c>CI</c>).</summary>
    public string Country         { get; private set; } = default!;

    /// <summary>Statut courant du marchand dans son cycle de vie.</summary>
    public MerchantStatus Status  { get; private set; }

    /// <summary>Plan tarifaire actif du marchand.</summary>
    public PricingPlan    Plan    { get; private set; }

    /// <summary>Date et heure UTC de création du compte marchand.</summary>
    public DateTimeOffset CreatedAt  { get; private set; }

    /// <summary>Date et heure UTC de validation KYB, ou <c>null</c> si le marchand n'est pas encore vérifié.</summary>
    public DateTimeOffset? VerifiedAt { get; private set; }
    
    /// <summary>Configuration du webhook de notification, ou <c>null</c> si aucun webhook n'est enregistré.</summary>
    public WebhookConfig?  WebhookConfig { get; private set; }

    /// <summary>Informations KYB soumises lors de la vérification, ou <c>null</c> avant soumission.</summary>
    public KybInfo?        KybInfo       { get; private set; }

    /// <summary>Limites opérationnelles déduites du plan tarifaire actif.</summary>
    public MerchantLimits  Limits        { get; private set; } = default!;
    
    // Auth
    public string?  PasswordHash    { get; private set; }
    public string?  RefreshToken    { get; private set; }
    public DateTimeOffset? RefreshTokenExpiresAt { get; private set; }

    private readonly List<ApiKey> _apiKeys = [];

    /// <summary>Clés d'API (Live et Sandbox) associées au marchand.</summary>
    public IReadOnlyList<ApiKey> ApiKeys => _apiKeys.AsReadOnly();

    /// <summary>
    /// Crée un nouveau marchand en statut <see cref="MerchantStatus.Pending"/> avec le plan <see cref="PricingPlan.Starter"/>.
    /// Une clé Live et une clé Sandbox sont générées automatiquement.
    /// </summary>
    public static (Merchant Merchant, string LiveKey, string SandboxKey) Create(string businessName, string email, string country)
    {
        var m = new Merchant
        {
            Id           = Guid.NewGuid(),
            BusinessName = businessName,
            Email        = email,
            Country      = country,
            Status       = MerchantStatus.Pending,
            Plan         = PricingPlan.Starter,
            CreatedAt    = DateTimeOffset.UtcNow,
            Limits       = MerchantLimits.ForPlan(PricingPlan.Starter),
        };
        
        var (liveKey,    liveApiKey)    = ApiKey.CreateLive(m.Id);
        var (sandboxKey, sandboxApiKey) = ApiKey.CreateSandbox(m.Id);
        m._apiKeys.Add(liveApiKey);
        m._apiKeys.Add(sandboxApiKey);
        
        return (m, liveKey, sandboxKey);
    }

    /// <summary>Enregistre ou remplace la configuration webhook du marchand.</summary>
    public void SetWebhook(string url, string secret) => WebhookConfig = new WebhookConfig(url, secret);

    /// <summary>Met à niveau le plan tarifaire et recalcule les limites associées.</summary>
    public void UpgradePlan(PricingPlan plan) { Plan = plan; Limits = MerchantLimits.ForPlan(plan); }

    /// <summary>Suspend le marchand : aucun paiement ne peut être traité tant qu'il est suspendu.</summary>
    public void Suspend() => Status = MerchantStatus.Suspended;
    
    public void Reactivate()
    {
        if (Status != MerchantStatus.Suspended)
            throw new InvalidOperationException("Only suspended merchants can be reactivated.");

        Status    = MerchantStatus.Active;
    }

    public void ChangePlan(PricingPlan newPlan)
    {
        if (Plan == newPlan) return;

        Plan      = newPlan;
    }
    
    
    /// <summary>Valide le KYB du marchand et le passe en statut <see cref="MerchantStatus.Active"/>.</summary>
    public void Verify(KybInfo kyb)
    {
        KybInfo = kyb; 
        Status = MerchantStatus.Active; 
        VerifiedAt = DateTimeOffset.UtcNow;
    }
    
    /// <summary>Définit le mot de passe lors de la création ou d'un reset.</summary>
    public void SetPassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new InvalidOperationException("Password hash cannot be empty.");
        PasswordHash = passwordHash;
    }
 
    /// <summary>Enregistre un Refresh Token après connexion réussie.</summary>
    public void SetRefreshToken(string token, DateTimeOffset expiresAt)
    {
        RefreshToken          = token;
        RefreshTokenExpiresAt = expiresAt;
    }
 
    /// <summary>Invalide le Refresh Token (logout).</summary>
    public void RevokeRefreshToken()
    {
        RefreshToken          = null;
        RefreshTokenExpiresAt = null;
    }
 
    public bool HasValidRefreshToken(string token)
        => RefreshToken == token
           && RefreshTokenExpiresAt.HasValue
           && RefreshTokenExpiresAt > DateTimeOffset.UtcNow;
}