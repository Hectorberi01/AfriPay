namespace AfriPay.Application.Analytics.Dtos;

public sealed record DailyStatsDto(
    DateOnly Date,
    int      Count,
    long     Volume,
    int      Completed,
    int      Failed);