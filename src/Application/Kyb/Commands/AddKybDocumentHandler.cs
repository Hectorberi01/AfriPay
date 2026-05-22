using AfriPay.Application.Common.Errors;
using AfriPay.Application.Kyb.Dtos;
using AfriPay.Domain.Kyb;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Kyb.Commands;

public sealed record AddKybDocumentCommand(
    Guid         MerchantId,
    DocumentType Type,
    string       FileName,
    string       StorageKey,
    string       ContentType,
    long         FileSizeBytes);

public sealed class AddKybDocumentHandler(IUnitOfWork uow)
{
    public async Task<Result<KybDto>> HandleAsync(
        AddKybDocumentCommand cmd, CancellationToken ct = default)
    {
        var kyb = await uow.Kyb.GetByMerchantAsync(cmd.MerchantId, ct);
        if (kyb is null)
            return Result<KybDto>.Fail(
                AppError.NotFound("KybApplication", cmd.MerchantId.ToString()));
 
        try
        {
            kyb.AddDocument(
                cmd.Type,
                cmd.FileName,
                cmd.StorageKey,
                cmd.ContentType,
                cmd.FileSizeBytes);
 
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