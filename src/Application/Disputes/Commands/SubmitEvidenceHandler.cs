using AfriPay.Application.Common.Errors;
using AfriPay.Application.Disputes.Dtos;
using AfriPay.Domain.Disputes;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Disputes.Commands;
public sealed record SubmitEvidenceCommand(
    Guid         MerchantId,
    Guid         DisputeId,
    string       EvidenceType,
    string       FileName,
    string       StorageKey,
    string?      MerchantNote,
    string?      Description);

public sealed class SubmitEvidenceHandler(IUnitOfWork uow)
{
    public async Task<Result<DisputeDto>> HandleAsync(
        SubmitEvidenceCommand cmd, CancellationToken ct = default)
    {
        var dispute = await uow.Disputes.GetByIdAsync(cmd.DisputeId, ct);
        if (dispute is null)
            return Result<DisputeDto>.Fail(
                AppError.NotFound("Dispute", cmd.DisputeId.ToString()));
        if (dispute.MerchantId != cmd.MerchantId)
            return Result<DisputeDto>.Fail(AppError.Unauthorized());
 
        if (!Enum.TryParse<EvidenceType>(cmd.EvidenceType, ignoreCase: true, out var type))
            return Result<DisputeDto>.Fail(
                AppError.Validation("evidenceType",
                    $"Valid types: {string.Join(", ", Enum.GetNames<EvidenceType>())}."));
 
        try
        {
            dispute.SubmitEvidence(type, cmd.FileName, cmd.StorageKey,
                cmd.MerchantNote, cmd.Description);
        }
        catch (DisputeDomainException ex)
        {
            return Result<DisputeDto>.Fail(AppError.DomainRule(ex.Message));
        }
 
        uow.Disputes.Update(dispute);
        await uow.SaveChangesAsync(ct);
        return Result<DisputeDto>.Ok(DisputeDto.FromDomain(dispute));
    }
}