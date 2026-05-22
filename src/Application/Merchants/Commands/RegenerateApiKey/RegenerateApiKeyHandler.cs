using AfriPay.Application.Common.Errors;
using AfriPay.Domain.Merchants;
using AfriPay.Domain.Repositories;
using MediatR;

namespace AfriPay.Application.Merchants.Commands.RegenerateApiKey;

public sealed class RegenerateApiKeyHandler(IUnitOfWork uow)
    : IRequestHandler<RegenerateApiKeyCommand, Result<RegenerateApiKeyResponse>>
{
    public async Task<Result<RegenerateApiKeyResponse>> Handle(
        RegenerateApiKeyCommand cmd,
        CancellationToken       ct)
    {
        var merchant = await uow.Merchants.GetByIdAsync(cmd.MerchantId, ct);
 
        if (merchant is null)
            return Result<RegenerateApiKeyResponse>.Fail(
                AppError.NotFound("Merchant", cmd.MerchantId.ToString()));
 
        var keyType = cmd.KeyType.Equals("live", StringComparison.OrdinalIgnoreCase)
            ? ApiKeyType.Live
            : ApiKeyType.Sandbox;
 
        // Révoquer l'ancienne clé
        var existing = merchant.ApiKeys.FirstOrDefault(k => k.Type == keyType && k.IsActive);
        existing?.Revoke();
 
        // Créer une nouvelle clé
        var (newKey,entity) = keyType == ApiKeyType.Live
            ? ApiKey.CreateLive(merchant.Id)
            : ApiKey.CreateSandbox(merchant.Id);

        
 
        // Ajouter via réflexion sur le backing field (encapsulation DDD)
        var field = typeof(Merchant)
            .GetField("_apiKeys", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field?.GetValue(merchant) is List<ApiKey> keys)
            keys.Add(entity);
 
        uow.Merchants.Update(merchant);
        await uow.SaveChangesAsync(ct);
 
        return Result<RegenerateApiKeyResponse>.Ok(
            new RegenerateApiKeyResponse(cmd.KeyType, newKey));
    }
}