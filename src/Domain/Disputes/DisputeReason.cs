namespace AfriPay.Domain.Disputes;

public enum DisputeReason
{
    Fraudulent, 
    Duplicate, 
    ProductNotReceived, 
    ProductUnacceptable,
    SubscriptionCancelled, 
    CreditNotProcessed, 
    General
}