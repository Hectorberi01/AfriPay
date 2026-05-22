using System.Text.Json.Serialization;

namespace AfriPay.API.Contracts.Responses.PayPal;

 
public sealed record PayPalOrderRequest(
    [property: JsonPropertyName("intent")]
    string Intent,
 
    [property: JsonPropertyName("purchase_units")]
    List<PayPalPurchaseUnit> PurchaseUnits,
 
    [property: JsonPropertyName("payment_source")]
    PayPalPaymentSource? PaymentSource = null
);