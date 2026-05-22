namespace AfriPay.Application.Notifications.Dtos;

public sealed record PaymentNotificationDto(
    string MerchantEmail,
    string MerchantName,
    string PaymentId,
    long   Amount,
    string Currency,
    string Provider,
    DateTimeOffset OccurredAt);
 
public sealed record RefundNotificationDto(
    string MerchantEmail,
    string MerchantName,
    string RefundId,
    string PaymentId,
    long   Amount,
    string Currency,
    string Reason);
 
public sealed record PayoutNotificationDto(
    string MerchantEmail,
    string MerchantName,
    string PayoutId,
    long   Amount,
    string Currency,
    string Method,
    string? ProviderReference,
    string? FailureReason);
 
public sealed record KybNotificationDto(
    string MerchantEmail,
    string MerchantName,
    string Status,
    string? Note);
