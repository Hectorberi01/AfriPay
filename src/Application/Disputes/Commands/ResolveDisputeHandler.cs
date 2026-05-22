using AfriPay.Application.Common.Errors;
using AfriPay.Application.Disputes.Dtos;
using AfriPay.Domain.Disputes;
using AfriPay.Domain.Exceptions;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Disputes.Commands;

public sealed record ResolveDisputeCommand(
    Guid    DisputeId,
    bool    MerchantWon,
    string? Note);
 
public sealed class ResolveDisputeHandler(IUnitOfWork uow)
{
    public async Task<Result<DisputeDto>> HandleAsync(
        ResolveDisputeCommand cmd, CancellationToken ct = default)
    {
        var dispute = await uow.Disputes.GetByIdAsync(cmd.DisputeId, ct);
        if (dispute is null)
            return Result<DisputeDto>.Fail(
                AppError.NotFound("Dispute", cmd.DisputeId.ToString()));
 
        try
        {
            if (cmd.MerchantWon)
            {
                dispute.MarkWon(cmd.Note);
            }
            else
            {
                dispute.MarkLost(cmd.Note);
 
                // Débiter le balance du marchand
                var balance = await uow.MerchantBalances
                    .GetByMerchantAndCurrencyAsync(
                        dispute.MerchantId, dispute.Currency, ct);
 
                if (balance is not null)
                {
                    try
                    {
                        var entry = balance.DebitForRefund(
                            dispute.TotalChargebackAmount,
                            dispute.Id.ToString());
                        uow.MerchantBalances.Update(balance);
                        await uow.BalanceEntries.AddAsync(entry, ct);
                    }
                    catch (DomainException)
                    {
                        // Solde insuffisant — noter mais ne pas bloquer
                    }
                }
            }
 
            uow.Disputes.Update(dispute);
            await uow.SaveChangesAsync(ct);
            return Result<DisputeDto>.Ok(DisputeDto.FromDomain(dispute));
        }
        catch (DisputeDomainException ex)
        {
            return Result<DisputeDto>.Fail(AppError.DomainRule(ex.Message));
        }
    }
}