namespace AfriPay.Application.Analytics.Dtos;

public sealed record PayoutSummaryDto(
    long   TotalPaidOut,
    int    PayoutCount,
    long   PendingPayout,
    string Currency);