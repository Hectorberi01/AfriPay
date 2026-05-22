namespace AfriPay.API.Contracts.Responses;

public sealed record ExchangeRateResponse
{
    public string          From          { get; init; } = default!;
    public string          To            { get; init; } = default!;
    public decimal         OfficialRate  { get; init; }
    public decimal         Spread        { get; init; }
    public decimal         EffectiveRate { get; init; }
    public string          Source        { get; init; } = default!;
    public DateTimeOffset  RecordedAt    { get; init; }
    public DateTimeOffset  ValidUntil    { get; init; }
 
    /// <summary>Exemple : 100 EUR = X XOF au taux effectif.</summary>
    public long SampleConversion100 { get; init; }
}