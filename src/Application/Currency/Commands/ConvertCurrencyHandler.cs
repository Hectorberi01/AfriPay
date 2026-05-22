using AfriPay.API.Contracts.Requests;
using AfriPay.Application.Common.Errors;
using AfriPay.Application.Currency.Dtos;
using AfriPay.Domain.Currency;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Currency.Commands;

public sealed class ConvertCurrencyHandler(IUnitOfWork uow)
{
    public async Task<Result<ConvertDto>> HandleAsync(ConvertCurrencyQuery query, CancellationToken ct = default)
    {
        if (query.Amount <= 0)
            return Result<ConvertDto>.Fail(
                AppError.Validation("amount", "Amount must be positive."));
 
        if (CurrencyConverter.SameZone(query.From, query.To))
            return Result<ConvertDto>.Fail(
                AppError.Validation("from/to", "XOF and XAF are in the same monetary zone."));
 
        CurrencyPair pair;
        try { pair = new CurrencyPair(query.From, query.To); }
        catch (CurrencyDomainException ex)
        {
            return Result<ConvertDto>.Fail(AppError.Validation("currency", ex.Message));
        }
 
        var rate = await uow.ExchangeRates.GetLatestValidAsync(pair, ct) ?? await uow.ExchangeRates.GetLastRecordedAsync(pair, ct);
 
        if (rate is null)
        {
            if (pair.From == "EUR" && pair.To == "XOF") rate = ExchangeRate.CreateEurXofFixed();
            else return Result<ConvertDto>.Fail(AppError.NotFound("ExchangeRate", $"{query.From}/{query.To}"));
        }
 
        if (rate.IsExpired) return Result<ConvertDto>.Fail(AppError.DomainRule($"Exchange rate for {pair} has expired. Please retry."));
 
        var converted = rate.Convert(query.Amount);
        var spread    = rate.SpreadAmount(query.Amount);
 
        return Result<ConvertDto>.Ok(new ConvertDto(
            query.Amount,
            query.From.ToUpperInvariant(),
            converted,
            query.To.ToUpperInvariant(),
            rate.EffectiveRate,
            spread,
            rate.RecordedAt));
    }
}