using System.Text.Json.Serialization;

namespace AfriPay.API.Contracts.Responses.PayPal;

internal sealed record PayPalTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token_type")]   string TokenType,
    [property: JsonPropertyName("expires_in")]   int    ExpiresIn);