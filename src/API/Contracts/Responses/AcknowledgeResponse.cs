namespace AfriPay.API.Contracts.Responses;

/// <summary>Réponse simple de confirmation sans corps de données.</summary>
public sealed record AcknowledgeResponse(
    string Message = "Operation completed successfully."
);
