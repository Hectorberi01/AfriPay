namespace AfriPay.Application.Analytics.Dtos;

public sealed record AnalyticsSummaryDto(
    DateTimeOffset From,
    DateTimeOffset To,
    long   GrossVolume,
    long   NetRevenue,
    long   TotalFees,
    string Currency,
    int    TotalTransactions,
    int    CompletedCount,
    int    FailedCount,
    int    RefundedCount,
    int    PendingCount,
    decimal SuccessRate,
    decimal RefundRate,
    long    AverageTransactionAmount,
    IReadOnlyList<ProviderStatsDto> ByProvider,
    IReadOnlyList<DailyStatsDto>    DailySeries);