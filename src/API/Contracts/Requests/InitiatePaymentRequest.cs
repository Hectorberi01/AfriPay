namespace AfriPay.API.Contracts.Requests;

/// <summary>
/// Requête POST /v1/payments/initiate
/// </summary>
public sealed record InitiatePaymentRequest
{
    /// <summary>
    /// Montant en unités minimales.
    /// XOF : entier (5000 = 5 000 XOF).
    /// EUR : centimes (1200 = 12,00 EUR).
    /// </summary>
    public long Amount { get; init; }
 
    /// <summary>Code devise ISO 4217 : XOF, EUR, USD, GHS.</summary>
    public string Currency { get; init; } = default!;
 
    /// <summary>
    /// ID provider ou "auto" pour le smart routing.
    /// Valeurs : mtn_momo | moov_money | wave | orange_money | stripe | paypal | auto
    /// </summary>
    public string Provider { get; init; } = default!;
 
    /// <summary>Informations du client payeur.</summary>
    public CustomerRequest? Customer { get; init; }
 
    /// <summary>Metadata libres transmises aux webhooks.</summary>
    public Dictionary<string, string>? Metadata { get; init; }
 
    /// <summary>
    /// URL de callback webhook pour ce paiement spécifique.
    /// Si absent, utilise la configuration globale du marchand.
    /// </summary>
    public string? WebhookUrl { get; init; }
}