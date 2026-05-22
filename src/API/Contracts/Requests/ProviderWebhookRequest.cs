namespace AfriPay.API.Contracts.Requests;

/// <summary>
/// Requête POST /webhooks/providers/{provider}
/// Body brut — chaque provider a son propre format.
/// La désérialisation est déléguée au IWebhookNormalizer correspondant.
/// </summary>
public sealed record ProviderWebhookRequest(string Provider);