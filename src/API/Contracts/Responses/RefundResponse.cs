namespace AfriPay.API.Contracts.Responses;

public sealed record RefundResponse
{
    public string          RefundId          { get; init; } = default!;
    public string          PaymentId         { get; init; } = default!;
    public string          Status            { get; init; } = default!;
    public string          Reason            { get; init; } = default!;
    public bool            IsPartial         { get; init; }
 
    // Montant
    public long            Amount            { get; init; }
    public string          Currency          { get; init; } = default!;
 
    public string?         ProviderReference { get; init; }
    public string?         Notes             { get; init; }
 
    public DateTimeOffset  CreatedAt         { get; init; }
    public DateTimeOffset  EstimatedArrival  { get; init; }
    public DateTimeOffset? CompletedAt       { get; init; }
    public DateTimeOffset? FailedAt          { get; init; }
}