namespace AfriPay.Domain.Payments;

public enum PaymentStatus
{
    Pending,    // Initié, en attente de confirmation du payeur
    Completed,  // Paiement confirmé et fonds reçus
    Failed,     // Échec (solde insuffisant, refus, erreur provider)
    Cancelled,  // Annulé par le marchand avant confirmation
    Expired,    // Délai de 30 min dépassé sans confirmation
    Refunded,   // Remboursé intégralement
}