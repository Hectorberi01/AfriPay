using System.Text.Json.Serialization;

namespace AfriPay.API.Contracts.Responses.PayPal;

public sealed record PayPalPaymentSource(
    [property: JsonPropertyName("paypal")]
    PayPalWallet? Wallet = null
);