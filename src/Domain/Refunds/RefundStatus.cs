namespace AfriPay.Domain.Refunds;

public enum RefundStatus
{
    Pending,    // Demande créée, en cours de traitement côté provider
    Processing,
    Completed,  // Remboursement confirmé par le provider
    Failed,     // Échec du remboursement (voir Notes pour la raison)
}