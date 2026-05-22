using System.Text.Json.Serialization;

namespace AfriPay.API.Contracts.Responses.PayPal;

public sealed record PayPalExperienceContext(
    [property: JsonPropertyName("return_url")]
    string? ReturnUrl = null,
 
    [property: JsonPropertyName("cancel_url")]
    string? CancelUrl = null,
 
    [property: JsonPropertyName("user_action")]
    string UserAction = "PAY_NOW"
);