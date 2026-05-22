using AfriPay.Application.Common.Errors;
using AfriPay.Application.Kyb.Dtos;
using AfriPay.Domain.Kyb;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Kyb.Commands;

public sealed record SetBusinessInfoCommand(
    Guid    MerchantId,
    string  LegalName,
    string? RegistrationNumber,
    string? TaxId,
    string? BusinessType,
    string? Website,
    string? LegalRepresentativeName,
    string? LegalRepresentativeEmail);

public sealed class SetBusinessInfoHandler(IUnitOfWork uow)
{
    public async Task<Result<KybDto>> HandleAsync(
        SetBusinessInfoCommand cmd, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cmd.LegalName))
            return Result<KybDto>.Fail(
                AppError.Validation("legalName", "Legal name is required."));
 
        var kyb = await uow.Kyb.GetByMerchantAsync(cmd.MerchantId, ct);
        if (kyb is null)
            return Result<KybDto>.Fail(
                AppError.NotFound("KybApplication", cmd.MerchantId.ToString()));
 
        try
        {
            kyb.SetBusinessInfo(
                cmd.LegalName,
                cmd.RegistrationNumber,
                cmd.TaxId,
                cmd.BusinessType,
                cmd.Website);
 
            if (cmd.LegalRepresentativeName is not null)
                kyb.SetLegalRepresentative(
                    cmd.LegalRepresentativeName,
                    cmd.LegalRepresentativeEmail ?? "");
 
            uow.Kyb.Update(kyb);
            await uow.SaveChangesAsync(ct);
 
            return Result<KybDto>.Ok(KybDto.FromDomain(kyb));
        }
        catch (KybDomainException ex)
        {
            return Result<KybDto>.Fail(AppError.DomainRule(ex.Message));
        }
    }
}