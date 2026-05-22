using System.Security.Cryptography;

namespace AfriPay.Domain.Merchants;

/// <summary>Distingue les clés d'API de production des clés de test (sandbox).</summary>
public enum ApiKeyType { Live, Sandbox }

/// <summary>
/// Clé d'API associée à un marchand, stockée sous forme de hash SHA-256.
/// Seul le préfixe lisible est conservé en clair ; la valeur brute n'est jamais persistée.
/// </summary>
public class ApiKey
{
    /// <summary>Identifiant unique de la clé.</summary>
    public Guid       Id         { get; private set; }

    /// <summary>Identifiant du marchand propriétaire de la clé.</summary>
    public Guid       MerchantId { get; private set; }

    /// <summary>Hash SHA-256 de la clé brute, utilisé pour l'authentification.</summary>
    public string     KeyHash    { get; private set; } = default!;

    /// <summary>Préfixe lisible permettant d'identifier la clé sans exposer sa valeur (ex. <c>afp_live_</c>).</summary>
    public string     Prefix     { get; private set; } = default!;

    /// <summary>Type de la clé : Live (production) ou Sandbox (test).</summary>
    public ApiKeyType Type       { get; private set; }

    /// <summary>Indique si la clé est utilisable. Passe à <c>false</c> après révocation.</summary>
    public bool       IsActive   { get; private set; }

    /// <summary>Date et heure UTC de création de la clé.</summary>
    public DateTimeOffset  CreatedAt  { get; private set; }
    
    // Clé brute conservée en mémoire UNIQUEMENT lors de la création.
    // Jamais persistée — null après chargement depuis la base.
    // Consommée une seule fois via GetRawKeyOnce().
    private string? _rawKey;

    /// <summary>Date et heure UTC de révocation, ou <c>null</c> si la clé est toujours active.</summary>
    public DateTimeOffset? RevokedAt  { get; private set; }

    /// <summary>Crée une clé de production pour le marchand spécifié.</summary>
    public static (string RawKey, ApiKey Entity) CreateLive(Guid merchantId)    => CreateKey(merchantId, "afp_live_", ApiKeyType.Live);

    /// <summary>Crée une clé sandbox pour le marchand spécifié.</summary>
    public static (string RawKey, ApiKey Entity) CreateSandbox(Guid merchantId) => CreateKey(merchantId, "afp_test_", ApiKeyType.Sandbox);

    private static  (string RawKey, ApiKey Entity) CreateKey(Guid merchantId, string prefix, ApiKeyType type)
    {
        var bytes   = RandomNumberGenerator.GetBytes(32);
        var b64url  = Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
        
        var raw  = $"{prefix}sk_{b64url}";
        var hash = Convert.ToHexString(
            SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
        var entity = new ApiKey
        {
            Id = Guid.NewGuid(),
            MerchantId = merchantId, 
            KeyHash = hash, 
            Prefix = prefix, 
            Type = type, 
            IsActive = true, 
            CreatedAt = DateTimeOffset.UtcNow
        };
        
        return (raw, entity);
    }

    /// <summary>Révoque la clé : elle ne peut plus être utilisée pour s'authentifier.</summary>
    public void Revoke() { IsActive = false; RevokedAt = DateTimeOffset.UtcNow; }
    
    /// <summary>
    /// Retourne la clé API brute et l'efface de la mémoire.
    /// Peut être appelée UNE SEULE FOIS juste après Create().
    /// Retourne null si la clé a déjà été consommée ou si l'entité
    /// a été rechargée depuis la base.
    /// </summary>
    public string GetRawKeyOnce()
    {
        var key  = _rawKey ?? throw new InvalidOperationException(
            "Raw key is no longer available. It can only be retrieved once, immediately after creation.");
        _rawKey  = null;
        return key;
    }
}