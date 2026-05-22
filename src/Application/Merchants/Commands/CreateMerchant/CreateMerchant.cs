using AfriPay.API.Contracts.Responses;
using AfriPay.Application.Common.Errors;
using AfriPay.Domain.Merchants;
using AfriPay.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace AfriPay.Application.Merchants.Commands.CreateMerchant;


/// <summary>
/// Retourne les clés API en clair UNE SEULE FOIS à la création.
/// Elles ne sont plus jamais renvoyées après — stockage sécurisé obligatoire.
/// </summary>

public sealed class CreateMerchantValidator : AbstractValidator<CreateMerchantCommand>
{
    public CreateMerchantValidator()
    {
        RuleFor(x => x.BusinessName)
            .NotEmpty().WithMessage("Business name is required.")
            .MaximumLength(255);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(255);

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .Length(2).WithMessage("Country must be a 2-char ISO 3166-1 code (e.g. FR, BJ, CI).");
    }
}


public sealed class CreateMerchantHandler(IUnitOfWork uow) : IRequestHandler<CreateMerchantCommand, Result<CreateMerchantResponse>>
{
    public async Task<Result<CreateMerchantResponse>> Handle(CreateMerchantCommand cmd, CancellationToken ct)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(cmd.BusinessName))
            return Result<CreateMerchantResponse>.Fail(
                AppError.Validation("businessName", "Business name is required."));

        if (string.IsNullOrWhiteSpace(cmd.Email) || !cmd.Email.Contains('@'))
            return Result<CreateMerchantResponse>.Fail(
                AppError.Validation("email", "A valid email is required."));

        if (string.IsNullOrWhiteSpace(cmd.Country) || cmd.Country.Length != 2)
            return Result<CreateMerchantResponse>.Fail(
                AppError.Validation("country", "Country must be a 2-char ISO 3166-1 code."));

        // Unicité email
        if (await uow.Merchants.ExistsByEmailAsync(cmd.Email, ct))
            return Result<CreateMerchantResponse>.Fail(
                AppError.Conflict($"An account with email '{cmd.Email}' already exists."));

        // Créer l'agrégat — clés brutes dans le tuple
        var (merchant, liveKey, sandboxKey) = Merchant.Create(
            cmd.BusinessName,
            cmd.Email,
            cmd.Country);

        await uow.Merchants.AddAsync(merchant, ct);
        await uow.SaveChangesAsync(ct);

        return Result<CreateMerchantResponse>.Ok(new CreateMerchantResponse{
            MerchantId = merchant.Id.ToString(),
            LiveApiKey = liveKey,
            SandboxApiKey = sandboxKey
            
        });
    }
}