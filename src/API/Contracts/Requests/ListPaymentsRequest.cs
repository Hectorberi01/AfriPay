namespace AfriPay.API.Contracts.Requests;

/// <summary>
/// Filtres de liste GET /v1/payments
/// </summary>
public sealed record ListPaymentsRequest
{
    public string?         Status      { get; init; }
    public string?         Provider    { get; init; }
    public DateTimeOffset? From        { get; init; }
    public DateTimeOffset? To          { get; init; }
    public int             Page        { get; init; } = 1;
    public int             PageSize    { get; init; } = 20;
}