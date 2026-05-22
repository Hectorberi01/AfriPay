namespace AfriPay.Domain.Merchants.ValueObjects;

/// <summary>
/// Configuration du webhook de notification d'un marchand.
/// AfriPay signe chaque requête sortante avec le <paramref name="Secret"/> via HMAC-SHA256
/// afin que le marchand puisse en vérifier l'authenticité.
/// </summary>
/// <param name="Url">URL HTTPS de l'endpoint du marchand qui recevra les événements.</param>
/// <param name="Secret">Secret partagé utilisé pour la signature HMAC des payloads.</param>
public sealed record WebhookConfig(string Url, string Secret);