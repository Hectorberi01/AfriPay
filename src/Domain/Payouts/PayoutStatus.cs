namespace AfriPay.Domain.Payouts;

public enum PayoutStatus
{
    Pending,        // Créé, en attente de traitement
    Processing,     // En cours d'envoi au provider
    Completed,      // Fonds reçus par le marchand
    Failed,         // Échec — solde non débité
    Cancelled,      // Annulé avant traitement
}