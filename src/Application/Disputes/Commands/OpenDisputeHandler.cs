using AfriPay.Application.Common.Errors;
using AfriPay.Application.Disputes.Dtos;
using AfriPay.Domain.Audit;
using AfriPay.Domain.Disputes;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Disputes.Commands;

public sealed record OpenDisputeCommand(
    Guid          MerchantId,
    Guid          PaymentId,
    long          Amount,
    string        Currency,
    string        Reason,
    string?       ProviderReference,
    string?       ProviderKey,
    string?       CustomerNote);
 
public sealed class OpenDisputeHandler(IUnitOfWork uow)
{
    public async Task<Result<DisputeDto>> HandleAsync(
        OpenDisputeCommand cmd, CancellationToken ct = default)
    {
        var payment = await uow.Payments.GetByIdAsync(cmd.PaymentId, ct);
        if (payment is null)
            return Result<DisputeDto>.Fail(
                AppError.NotFound("Payment", cmd.PaymentId.ToString()));
 
        if (!Enum.TryParse<DisputeReason>(cmd.Reason, ignoreCase: true, out var reason))
            return Result<DisputeDto>.Fail(
                AppError.Validation("reason",
                    $"Valid reasons: {string.Join(", ", Enum.GetNames<DisputeReason>())}."));
 
        var dispute = Dispute.Open(
            cmd.MerchantId, cmd.PaymentId, cmd.Amount, cmd.Currency,
            reason, cmd.ProviderReference, cmd.ProviderKey, cmd.CustomerNote);
 
        await uow.Disputes.AddAsync(dispute, ct);
 
        // Log audit
        await uow.Audit.AddAsync(AuditEntry.System(
            cmd.MerchantId, AuditAction.PaymentFailed,
            "Dispute", dispute.Id.ToString(),
            $"{{\"paymentId\":\"{cmd.PaymentId}\",\"reason\":\"{reason}\"}}"), ct);
 
        await uow.SaveChangesAsync(ct);
        return Result<DisputeDto>.Ok(DisputeDto.FromDomain(dispute));
    }
}