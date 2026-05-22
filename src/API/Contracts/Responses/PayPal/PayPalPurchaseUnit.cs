using System.Text.Json.Serialization;

namespace AfriPay.API.Contracts.Responses.PayPal;

public sealed record PayPalPurchaseUnit(
    [property: JsonPropertyName("amount")] PayPalAmount Amount,
 
    [property: JsonPropertyName("reference_id")]
    string? ReferenceId = null,
 
    [property: JsonPropertyName("description")]
    string? Description = null
);