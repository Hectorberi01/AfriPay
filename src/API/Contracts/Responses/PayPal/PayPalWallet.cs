using System.Text.Json.Serialization;

namespace AfriPay.API.Contracts.Responses.PayPal;

public sealed record PayPalWallet(
    [property: JsonPropertyName("experience_context")]
    PayPalExperienceContext? ExperienceContext = null
);