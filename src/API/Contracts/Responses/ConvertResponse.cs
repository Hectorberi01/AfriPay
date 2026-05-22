namespace AfriPay.API.Contracts.Responses;

 
/// <summary>Résultat d'une conversion de devises à la demande.</summary>
public sealed record ConvertResponse(
    long    SourceAmount,
    string  SourceCurrency,
    long    ConvertedAmount,
    string  TargetCurrency,
    decimal RateApplied,
    long    SpreadAmount,
    DateTimeOffset RateAt
);