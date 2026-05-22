namespace AfriPay.API.Contracts.Responses;

public sealed record MerchantResponse
{
    public string          MerchantId    { get; init; } = default!;
    public string          BusinessName  { get; init; } = default!;
    public string          Email         { get; init; } = default!;
    public string          Country       { get; init; } = default!;
    public string          Status        { get; init; } = default!;
    public string          Plan          { get; init; } = default!;
    public bool            HasWebhook    { get; init; }
    public bool            KybVerified   { get; init; }
    public DateTimeOffset  CreatedAt     { get; init; }
    public DateTimeOffset? VerifiedAt    { get; init; }
}