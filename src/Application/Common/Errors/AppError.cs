namespace AfriPay.Application.Common.Errors;

/// <summary>
/// Erreur métier normalisée retournée par les handlers CQRS.
/// Contient le code, le message lisible et le status HTTP correspondant.
///
/// Factories statiques couvrent tous les cas standards —
/// pas de construction manuelle dans les handlers.
/// </summary>
public sealed class AppError(string Code, string Message, int StatusCode = 400)
{
    public string Code       { get; } = Code;
    public string Message    { get; } = Message;
    public int    StatusCode { get; } = StatusCode;
 
    // Factories 
 
    /// <summary>404 — Ressource introuvable.</summary>
    public static AppError NotFound(string resource, string id) =>
        new($"{resource.ToUpper()}_NOT_FOUND",
            $"{resource} '{id}' not found.",
            404);
 
    /// <summary>409 — Conflit (doublon, état incompatible).</summary>
    public static AppError Conflict(string message) =>
        new("CONFLICT", message, 409);
 
    /// <summary>422 — Validation métier échouée.</summary>
    public static AppError Validation(string field, string message) =>
        new("VALIDATION_ERROR", $"{field}: {message}", 422);
 
    /// <summary>401 — Clé API invalide ou absente.</summary>
    public static AppError Unauthorized() =>
        new("UNAUTHORIZED", "Invalid or missing API key.", 401);
    
    public static AppError Unauthorized(string error,string errorCode) =>
        new(error, errorCode);

 
    /// <summary>403 — Accès refusé (ressource appartient à un autre marchand).</summary>
    public static AppError Forbidden(string resource) =>
        new("FORBIDDEN",
            $"You do not have access to this {resource}.",
            403);
 
    /// <summary>502 — Erreur retournée par un provider de paiement.</summary>
    public static AppError ProviderError(string providerKey, string detail) =>
        new("PROVIDER_ERROR",
            $"Provider '{providerKey}' returned an error: {detail}",
            502);
 
    /// <summary>503 — Provider temporairement indisponible (circuit ouvert).</summary>
    public static AppError ProviderUnavailable(string providerKey) =>
        new("PROVIDER_UNAVAILABLE",
            $"Provider '{providerKey}' is temporarily unavailable. Try again later.",
            503);
 
    /// <summary>400 — Règle métier domaine violée.</summary>
    public static AppError DomainRule(string message) =>
        new("DOMAIN_RULE_VIOLATION", message, 400);
 
    // Helpers
 
    public override string ToString() => $"[{StatusCode}] {Code}: {Message}";
}
 