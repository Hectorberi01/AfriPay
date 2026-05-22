namespace AfriPay.API.Contracts.Requests;

/// <summary>
/// Requête PUT /v1/merchants/webhook
/// </summary>
public sealed record UpdateWebhookRequest
{
    public string  Url    { get; init; } = default!;
    public string? Secret { get; init; }  // Si null → régénéré automatiquement
}