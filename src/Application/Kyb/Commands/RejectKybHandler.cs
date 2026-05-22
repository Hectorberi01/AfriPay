using AfriPay.Application.Common.Errors;
using AfriPay.Application.Kyb.Dtos;
using AfriPay.Domain.Kyb;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Kyb.Commands;

public sealed record RejectKybCommand(
    Guid   KybId,
    string ReviewerEmail,
    string Reason);

public sealed class RejectKybHandler(IUnitOfWork uow)
{
    public async Task<Result<KybDto>> HandleAsync(
        RejectKybCommand cmd, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cmd.Reason))
            return Result<KybDto>.Fail(
                AppError.Validation("reason", "Rejection reason is required."));
 
        var kyb = await uow.Kyb.GetByIdAsync(cmd.KybId, ct);
        if (kyb is null)
            return Result<KybDto>.Fail(
                AppError.NotFound("KybApplication", cmd.KybId.ToString()));
 
        try
        {
            kyb.Reject(cmd.ReviewerEmail, cmd.Reason);
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