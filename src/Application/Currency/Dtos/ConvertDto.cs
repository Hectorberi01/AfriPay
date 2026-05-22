namespace AfriPay.Application.Currency.Dtos;

public sealed record ConvertDto(
    long            SourceAmount,
    string          SourceCurrency,
    long            ConvertedAmount,
    string          TargetCurrency,
    decimal         RateApplied,
    long            SpreadAmount,
    DateTimeOffset  RateAt);