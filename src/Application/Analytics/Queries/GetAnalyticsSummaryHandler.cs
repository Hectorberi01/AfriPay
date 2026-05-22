using AfriPay.Application.Analytics.Dtos;
using AfriPay.Application.Common.Errors;
using AfriPay.Application.Payments.Queries;
using AfriPay.Domain.Payments;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Analytics.Queries;
public sealed record GetAnalyticsSummaryQuery(
    Guid           MerchantId,
    string         Currency,
    DateTimeOffset From,
    DateTimeOffset To);

public sealed class GetAnalyticsSummaryHandler(IUnitOfWork uow)
{
    public async Task<Result<AnalyticsSummaryDto>> HandleAsync(
        GetAnalyticsSummaryQuery query, CancellationToken ct = default)
    {
        if (query.From >= query.To)
            return Result<AnalyticsSummaryDto>.Fail(
                AppError.Validation("from/to", "'from' must be before 'to'."));

        if ((query.To - query.From).TotalDays > 366)
            return Result<AnalyticsSummaryDto>.Fail(
                AppError.Validation("range", "Date range cannot exceed 366 days."));

        var stats = await uow.Payments.GetStatsAsync(
            query.MerchantId, query.From, query.To, ct);

        var paged = await uow.Payments.ListAsync(
            query.MerchantId,
            new PaymentQueryFilter(
                From: query.From,
                To:   query.To),
            page: 1, pageSize: 10_000, ct);

        var items = paged.Items;

        // Par provider
        var byProvider = items
            .GroupBy(p => p.ProviderKey)
            .Select(g =>
            {
                var total     = g.Count();
                var completed = g.Count(p => p.Status == PaymentStatus.Completed);
                return new ProviderStatsDto(
                    g.Key, total,
                    g.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount.Amount),
                    total > 0 ? Math.Round((decimal)completed / total * 100, 1) : 0);
            })
            .OrderByDescending(p => p.Volume)
            .ToList();

        // Série journalière
        var daily = items
            .GroupBy(p => DateOnly.FromDateTime(p.CreatedAt.LocalDateTime))
            .OrderBy(g => g.Key)
            .Select(g => new DailyStatsDto(
                g.Key, g.Count(),
                g.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount.Amount),
                g.Count(p => p.Status == PaymentStatus.Completed),
                g.Count(p => p.Status == PaymentStatus.Failed)))
            .ToList();

        // Noms alignés avec ton PaymentStats local
        var total     = stats.TransactionCount;
        var completed = stats.CompletedCount;
        var failed    = stats.FailedCount;
        var refunded  = items.Count(p => p.Status == PaymentStatus.Refunded);

        return Result<AnalyticsSummaryDto>.Ok(new AnalyticsSummaryDto(
            query.From, query.To,
            stats.GrossVolume,
            stats.GrossVolume - stats.TotalFees,
            stats.TotalFees,
            query.Currency.ToUpperInvariant(),
            total, completed, failed, refunded,
            total - completed - failed - refunded,
            total     > 0 ? Math.Round((decimal)completed / total     * 100, 1) : 0,
            completed > 0 ? Math.Round((decimal)refunded  / completed * 100, 1) : 0,
            completed > 0 ? stats.GrossVolume / completed : 0,
            byProvider, daily));
    }
}
