using System.Text.Json.Serialization;

namespace AfriPay.API.Contracts.Responses.PayPal;

public sealed record PayPalOrderResponse(
    [property: JsonPropertyName("id")]
    string Id,
 
    [property: JsonPropertyName("status")]
    string Status,
 
    [property: JsonPropertyName("links")]
    List<PayPalLink> Links
);