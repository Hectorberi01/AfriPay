namespace AfriPay.API.Contracts.Requests;

public record RegenerateApiKeyResponse(
    string KeyType,
    string NewKey,
    string Warning =
        "Store this key securely. It will not be shown again.");