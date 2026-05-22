namespace AfriPay.API.Contracts.Requests;

public sealed record CustomerRequest(
    /// <summary>Numéro de téléphone E.164 (requis pour Mobile Money).</summary>
    string? Phone,
    /// <summary>Email (utilisé pour les reçus et les providers carte).</summary>
    string? Email,
    /// <summary>Nom affiché sur certains providers (ex : PayPal).</summary>
    string? Name = null
);
