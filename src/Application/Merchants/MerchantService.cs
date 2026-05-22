using AfriPay.API.Contracts.Responses;
using AfriPay.Application.Common.Errors;
using AfriPay.Application.Merchants.Commands.CreateMerchant;
using AfriPay.Application.Merchants.Commands.RegenerateApiKey;
using AfriPay.Application.Merchants.Commands.UpdateWebhook;
using AfriPay.Application.Merchants.Queries.GetMerchant;

namespace AfriPay.Application.Merchants;


public interface IMerchantService
{
    Task<Result<CreateMerchantResponse>> CreateAsync(CreateMerchantCommand cmd, CancellationToken ct = default);
    Task<Result<MerchantResponse>> GetByIdAsync(Guid merchantId, CancellationToken ct = default);
    Task<Result<MerchantResponse>> UpdateWebhookAsync(UpdateWebhookCommand cmd, CancellationToken ct = default);
    Task<Result<RegenerateApiKeyResponse>> RegenerateApiKeyAsync(Guid merchantId, string keyType, CancellationToken ct = default);
    
}


public sealed class MerchantService(
    CreateMerchantHandler createMerchantHandler,
    RegenerateApiKeyHandler regenerateApiKeyHandler,
    UpdateWebhookHandler updateWebhookHandler,
    GetMerchantHandler getMerchantHandler
) : IMerchantService
{
    public async Task<Result<CreateMerchantResponse>> CreateAsync(
        CreateMerchantCommand cmd, CancellationToken ct = default)
         => await createMerchantHandler.Handle(cmd, ct);

    public async Task<Result<MerchantResponse>> GetByIdAsync(
        Guid merchantId, CancellationToken ct = default)
        => await getMerchantHandler.Handle(new GetMerchantQuery(merchantId) , ct);
    
    public async Task<Result<MerchantResponse>> UpdateWebhookAsync(
        UpdateWebhookCommand cmd, CancellationToken ct = default)
       => await updateWebhookHandler.Handle(cmd, ct);
    
    public async Task<Result<RegenerateApiKeyResponse>> RegenerateApiKeyAsync(
        Guid merchantId, string keyType, CancellationToken ct = default)
        => await regenerateApiKeyHandler.Handle(new RegenerateApiKeyCommand(merchantId,keyType), ct);
}