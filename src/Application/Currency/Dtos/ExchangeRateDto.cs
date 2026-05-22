namespace AfriPay.Application.Currency.Dtos;

public sealed record ExchangeRateDto(
    string          From,
    string          To,
    decimal         OfficialRate,
    decimal         Spread,
    decimal         EffectiveRate,
    string          Source,
    DateTimeOffset  RecordedAt,
    DateTimeOffset  ValidUntil,
    long            SampleConversion100);