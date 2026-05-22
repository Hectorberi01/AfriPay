namespace AfriPay.Domain.Balance;

public enum EntrySource
{
    Payment,       // Paiement complété par un client
    Refund,        // Remboursement émis vers un client
    Payout,        // Virement vers le compte bancaire du marchand
    Fee,           // Commission AfriPay prélevée
    Adjustment,    // Correction manuelle (admin)
    Chargeback,    // Contestation résolue en faveur du payeur
}