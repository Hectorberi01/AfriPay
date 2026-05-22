using AfriPay.Application.Analytics.Dtos;
using AfriPay.Application.Analytics.Queries;
using AfriPay.Application.Common.Errors;

namespace AfriPay.Application.Analytics;

public interface IAnalyticsService
{
    Task<Result<AnalyticsSummaryDto>> GetSummaryAsync(GetAnalyticsSummaryQuery query, CancellationToken ct = default);
    Task<Result<PayoutSummaryDto>> GetPayoutSummaryAsync(GetPayoutSummaryQuery query, CancellationToken ct = default);
}
 
public sealed class AnalyticsService(
    GetAnalyticsSummaryHandler summaryHandler,
    GetPayoutSummaryHandler    payoutHandler) : IAnalyticsService
{
    public Task<Result<AnalyticsSummaryDto>> GetSummaryAsync(
        GetAnalyticsSummaryQuery q, CancellationToken ct = default)
        => summaryHandler.HandleAsync(q, ct);
 
    public Task<Result<PayoutSummaryDto>> GetPayoutSummaryAsync(
        GetPayoutSummaryQuery q, CancellationToken ct = default)
        => payoutHandler.HandleAsync(q, ct);
}
