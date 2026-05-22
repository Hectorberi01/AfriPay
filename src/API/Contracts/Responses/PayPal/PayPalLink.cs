using System.Text.Json.Serialization;

namespace AfriPay.API.Contracts.Responses.PayPal;

public sealed record PayPalLink(
    [property: JsonPropertyName("href")]  string Href,
    [property: JsonPropertyName("rel")]   string Rel,
    [property: JsonPropertyName("method")] string Method
);