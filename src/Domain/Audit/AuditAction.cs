namespace AfriPay.Domain.Audit;

public enum AuditAction
{
    // Merchant
    MerchantCreated,
    MerchantUpdated,
    ApiKeyGenerated,
    ApiKeyRevoked,
    WebhookUpdated,
    PlanChanged,
 
    // Payment
    PaymentInitiated,
    PaymentCompleted,
    PaymentCancelled,
    PaymentFailed,
    PaymentRefunded,
 
    // Refund
    RefundCreated,
    RefundCompleted,
    RefundFailed,
 
    // Payout
    PayoutRequested,
    PayoutCompleted,
    PayoutFailed,
    PayoutCancelled,
 
    // KYB
    KybSubmitted,
    KybApproved,
    KybRejected,
 
    // Balance
    BalanceFrozen,
    BalanceUnfrozen,
 
    // Admin
    AdminLogin,
    AdminAction,
}
