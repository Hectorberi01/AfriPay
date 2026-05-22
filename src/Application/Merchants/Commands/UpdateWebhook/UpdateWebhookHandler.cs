using AfriPay.API.Contracts.Responses;
using AfriPay.Application.Common.Errors;
using AfriPay.Domain.Repositories;
using MediatR;

namespace AfriPay.Application.Merchants.Commands.UpdateWebhook;

public sealed class UpdateWebhookHandler(IUnitOfWork uow)
    : IRequestHandler<UpdateWebhookCommand, Result<MerchantResponse>>
{
    public async Task<Result<MerchantResponse>> Handle(
        UpdateWebhookCommand cmd,
        CancellationToken    ct)
    {
        var merchant = await uow.Merchants.GetByIdAsync(cmd.MerchantId, ct);
 
        if (merchant is null)
            return Result<MerchantResponse>.Fail(
                AppError.NotFound("Merchant", cmd.MerchantId.ToString()));
 
        if (!Uri.TryCreate(cmd.Url, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https"))
            return Result<MerchantResponse>.Fail(
                AppError.Validation("url", "Webhook URL must be a valid HTTPS URL."));
 
        // Génère un secret si non fourni
        var secret = string.IsNullOrWhiteSpace(cmd.Secret)
            ? Convert.ToHexString(
                System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
            : cmd.Secret;
 
        merchant.SetWebhook(cmd.Url, secret);
        uow.Merchants.Update(merchant);
        await uow.SaveChangesAsync(ct);
 
        return Result<MerchantResponse>.Ok(new MerchantResponse
        {
            MerchantId   = merchant.Id.ToString(),
            BusinessName = merchant.BusinessName,
            Email        = merchant.Email,
            Country      = merchant.Country,
            Status       = merchant.Status.ToString().ToLower(),
            Plan         = merchant.Plan.ToString().ToLower(),
            HasWebhook   = true,
            KybVerified  = merchant.KybInfo is not null,
            CreatedAt    = merchant.CreatedAt,
            VerifiedAt   = merchant.VerifiedAt,
        });
    }
}
