namespace AfriPay.Domain.Balance;

public enum EntryType
{
    Credit,   // Entrée d'argent  : paiement complété, remboursement annulé
    Debit,    // Sortie d'argent  : remboursement, payout, commission
}