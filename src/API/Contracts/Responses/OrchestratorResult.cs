namespace AfriPay.API.Contracts.Responses;

public sealed record OrchestratorResult(
    string  ProviderKey, 
    string? ProviderReference,
    bool    IsSuccess, 
    string? ErrorCode, 
    string? ErrorMessage,
    string? UssdCode,
    string? RedirectUrl
    );