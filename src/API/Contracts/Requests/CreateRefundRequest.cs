namespace AfriPay.API.Contracts.Requests;

/// <summary>
/// Requête POST /v1/payments/{paymentId}/refunds
/// </summary>
public sealed record CreateRefundRequest
{
    /// <summary>
    /// Montant à rembourser en unités minimales.
    /// Si absent → remboursement total du paiement original.
    /// </summary>
    public long? Amount { get; init; }
 
    /// <summary>
    /// Raison du remboursement.
    /// Valeurs : duplicate | fraudulent | customer_request | other
    /// </summary>
    public string Reason { get; init; } = "customer_request";
 
    /// <summary>Note libre (visible dans le dashboard marchand).</summary>
    public string? Notes { get; init; }
}