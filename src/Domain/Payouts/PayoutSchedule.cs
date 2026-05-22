namespace AfriPay.Domain.Payouts;

public enum PayoutSchedule
{
    Manual,         // Déclenché manuellement par le marchand
    Daily,          // Automatique chaque jour ouvrable
    Weekly,         // Automatique chaque lundi
    Monthly        // Automatique le 1er du mois
}