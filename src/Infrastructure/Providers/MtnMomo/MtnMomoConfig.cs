namespace AfriPay.Infrastructure.Providers.MtnMomo;

public sealed class MtnMomoConfig
{
    public const string SectionName = "Providers:MtnMomo";
    
    // Sandbox 
    public string SandboxBaseUrl          { get; init; } = "https://sandbox.momodeveloper.mtn.com/";
    public string SandboxSubscriptionKey  { get; init; } = "";
    public string SandboxApiUserId        { get; init; } = "";
    public string SandboxApiKey           { get; init; } = "";
    
    // Live 
    public string LiveBaseUrl             { get; init; } = "https://momodeveloper.mtn.com/";
    public string LiveSubscriptionKey     { get; init; } = "";
    public string LiveApiUserId           { get; init; } = "";
    public string LiveApiKey              { get; init; } = "";
    
    public string GetBaseUrl(bool isLive)         => isLive ? LiveBaseUrl         : SandboxBaseUrl;
    public string GetSubscriptionKey(bool isLive) => isLive ? LiveSubscriptionKey : SandboxSubscriptionKey;
    public string GetApiUserId(bool isLive)       => isLive ? LiveApiUserId       : SandboxApiUserId;
    public string GetApiKey(bool isLive)          => isLive ? LiveApiKey          : SandboxApiKey;
    public string GetTargetEnvironment(bool isLive) => isLive ? "mtnzambia" : "sandbox";

    public int TimeoutSeconds { get; init; } = 30;
    
    public required string BaseUrl            { get; init; }
}