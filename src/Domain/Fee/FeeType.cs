namespace AfriPay.Domain.Fee;

public enum FeeType
{
    Percentage,    // % du montant transactionnel
    Fixed,         // Montant fixe en unités minimales
    Mixed,         // Percentage + Fixed cumulés
}