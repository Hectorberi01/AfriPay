namespace AfriPay.Domain.Refunds;

public enum RefundReason
{
    Duplicate,        // Transaction en double
    Fraudulent,       // Fraude détectée
    CustomerRequest,  // Demande explicite du client
    Other,            // Autre raison (précisée dans Notes)
}