using AfriPay.Application.Common.Errors;
using AfriPay.Application.Kyb.Dtos;
using AfriPay.Domain.Kyb;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Kyb.Commands;

 
public sealed record InitKybCommand(Guid MerchantId, string Country);
 
public sealed class InitKybHandler(IUnitOfWork uow)
{
    public async Task<Result<KybDto>> HandleAsync(
        InitKybCommand cmd, CancellationToken ct = default)
    {
        // Idempotent — retourne l'existant si déjà créé
        var existing = await uow.Kyb.GetByMerchantAsync(cmd.MerchantId, ct);
        if (existing is not null)
            return Result<KybDto>.Ok(KybDto.FromDomain(existing));
 
        var kyb = KybApplication.Create(cmd.MerchantId, cmd.Country);
        await uow.Kyb.AddAsync(kyb, ct);
        await uow.SaveChangesAsync(ct);
 
        return Result<KybDto>.Ok(KybDto.FromDomain(kyb));
    }
}