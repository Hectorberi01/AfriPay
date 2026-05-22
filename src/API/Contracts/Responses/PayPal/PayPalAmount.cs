using System.Text.Json.Serialization;

namespace AfriPay.API.Contracts.Responses.PayPal;

public sealed record PayPalAmount(
    [property: JsonPropertyName("currency_code")]
    string CurrencyCode,
 
    [property: JsonPropertyName("value")]
    string Value 
);