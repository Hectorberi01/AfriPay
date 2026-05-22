using AfriPay.Application.Common.Errors;
using AfriPay.Application.Kyb.Dtos;
using AfriPay.Domain.Kyb;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Kyb.Commands;

public sealed record ApproveKybCommand(
    Guid    KybId,
    string  ReviewerEmail,
    string? Note);

public sealed class ApproveKybHandler(IUnitOfWork uow)
{
    public async Task<Result<KybDto>> HandleAsync(
        ApproveKybCommand cmd, CancellationToken ct = default)
    {
        var kyb = await uow.Kyb.GetByIdAsync(cmd.KybId, ct);
        if (kyb is null)
            return Result<KybDto>.Fail(
                AppError.NotFound("KybApplication", cmd.KybId.ToString()));
 
        try
        {
            kyb.Approve(cmd.ReviewerEmail, cmd.Note);
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