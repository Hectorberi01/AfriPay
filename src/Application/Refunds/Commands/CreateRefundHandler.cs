using AfriPay.API.Contracts.Requests;
using AfriPay.Application.Common.Errors;
using AfriPay.Domain.Payments;
using AfriPay.Domain.Payments.ValueObjects;
using AfriPay.Domain.Refunds;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Refunds.Commands;



public sealed class CreateRefundHandler(IUnitOfWork uow)
{
    public async Task<Result<RefundDto>> HandleAsync(
        CreateRefundCommand cmd, CancellationToken ct = default)
    {
        // Charger le paiement
        var payment = await uow.Payments.GetByIdAsync(cmd.PaymentId, ct);
 
        if (payment is null)
            return Result<RefundDto>.Fail(
                AppError.NotFound("Payment", cmd.PaymentId.ToString()));
 
        if (payment.MerchantId != cmd.MerchantId)
            return Result<RefundDto>.Fail(AppError.Unauthorized());
 
        if (payment.Status != PaymentStatus.Completed)
            return Result<RefundDto>.Fail(
                AppError.Conflict(
                    $"Cannot refund a payment in status '{payment.Status}'. " +
                    "Only completed payments can be refunded."));
 
        // Valider le montant partiel
        var refundAmount = cmd.Amount.HasValue
            ? new Money(cmd.Amount.Value, payment.Amount.Currency)
            : (Money?)null;
 
        if (refundAmount is not null && refundAmount.Amount > payment.Amount.Amount)
            return Result<RefundDto>.Fail(
                AppError.Validation("amount",
                    $"Refund amount ({refundAmount.Amount}) cannot exceed " +
                    $"payment amount ({payment.Amount.Amount})."));
 
        // Vérifier que le total des remboursements ne dépasse pas le montant original
        var alreadyRefunded = await uow.Refunds.GetTotalRefundedAmountAsync(cmd.PaymentId, ct);
        var requestedAmount = refundAmount?.Amount ?? payment.Amount.Amount;
 
        if (alreadyRefunded + requestedAmount > payment.Amount.Amount)
            return Result<RefundDto>.Fail(
                AppError.Conflict(
                    $"Total refunds ({alreadyRefunded + requestedAmount} {payment.Amount.Currency}) " +
                    $"would exceed payment amount ({payment.Amount.Amount} {payment.Amount.Currency})."));
 
        // Parser la raison
        if (!Enum.TryParse<RefundReason>(
                ToPascalCase(cmd.Reason), ignoreCase: true, out var reason))
            return Result<RefundDto>.Fail(
                AppError.Validation("reason",
                    "Reason must be: duplicate, fraudulent, customer_request or other."));
 
        // Créer l'agrégat
        var refund = Refund.Create(
            paymentId:     cmd.PaymentId,
            merchantId:    cmd.MerchantId,
            paymentAmount: payment.Amount,
            refundAmount:  refundAmount,
            reason:        reason,
            providerKey:   payment.ProviderUsed ?? payment.ProviderKey,
            notes:         cmd.Notes);
 
        await uow.Refunds.AddAsync(refund, ct);
        await uow.SaveChangesAsync(ct);
 
        return Result<RefundDto>.Ok(RefundDto.FromDomain(refund));
    }
 
    private static string ToPascalCase(string s) =>
        string.IsNullOrEmpty(s)
            ? s
            : string.Concat(
                s.Split('_')
                 .Select(w => char.ToUpper(w[0]) + w[1..].ToLower()));
}