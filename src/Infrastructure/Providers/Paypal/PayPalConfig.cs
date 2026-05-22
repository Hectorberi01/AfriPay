namespace AfriPay.Infrastructure.Providers.Paypal;

public sealed class PayPalConfig
{
    public const string SectionName = "Providers:PayPal";
 
    public string SandboxClientId     { get; init; } = "";
    public string SandboxClientSecret { get; init; } = "";
    public string LiveClientId        { get; init; } = "";
    public string LiveClientSecret    { get; init; } = "";
    public int    TimeoutSeconds      { get; init; } = 30;
 
    public string GetBaseUrl(bool isLive)
        => isLive
            ? "https://api-m.paypal.com/"
            : "https://api-m.sandbox.paypal.com/";
 
    public string GetClientId(bool isLive) => isLive ? LiveClientId     : SandboxClientId;
 
    public string GetClientSecret(bool isLive) => isLive ? LiveClientSecret : SandboxClientSecret;
}

