namespace AfriPay.Application.Analytics.Dtos;

public sealed record ProviderStatsDto(
    string  ProviderKey,
    int     Count,
    long    Volume,
    decimal SuccessRate);