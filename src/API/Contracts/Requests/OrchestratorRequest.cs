namespace AfriPay.API.Contracts.Requests;

public sealed record OrchestratorRequest(
    long   Amount, 
    string Currency, 
    string IdempotencyKey,
    string? PhoneNumber, 
    string? Email, 
    Dictionary<string, string> Metadata,
    bool IsLive = false
);