using AfriPay.API.Contracts.Requests;
using AfriPay.Application.Common.Errors;
using AfriPay.Application.Currency.Dtos;
using AfriPay.Domain.Currency;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Currency.Queries;

public sealed class GetExchangeRateHandler(IUnitOfWork uow)
{
    public async Task<Result<ExchangeRateDto>> HandleAsync(
        GetExchangeRateQuery query, CancellationToken ct = default)
    {
        if (query.From.Equals(query.To, StringComparison.OrdinalIgnoreCase))
            return Result<ExchangeRateDto>.Fail(
                AppError.Validation("from/to", "Source and target currencies must be different."));
 
        // Cas spécial EUR/XOF — taux fixe officiel BCEAO
        if (CurrencyConverter.SameZone(query.From, query.To))
            return Result<ExchangeRateDto>.Fail(
                AppError.Validation("from/to", "XOF and XAF are in the same monetary zone — no conversion needed."));
 
        CurrencyPair pair;
        try { pair = new CurrencyPair(query.From, query.To); }
        catch (CurrencyDomainException ex)
        {
            return Result<ExchangeRateDto>.Fail(AppError.Validation("currency", ex.Message));
        }
 
        var rate = await uow.ExchangeRates.GetLatestValidAsync(pair, ct)
                   ?? await uow.ExchangeRates.GetLastRecordedAsync(pair, ct);
 
        if (rate is null)
        {
            // EUR/XOF fixe — toujours disponible même sans entrée en base
            if (pair.From == "EUR" && pair.To == "XOF")
                rate = ExchangeRate.CreateEurXofFixed();
            else
                return Result<ExchangeRateDto>.Fail(
                    AppError.NotFound("ExchangeRate", $"{query.From}/{query.To}"));
        }
 
        return Result<ExchangeRateDto>.Ok(new ExchangeRateDto(
            rate.Pair.From,
            rate.Pair.To,
            rate.OfficialRate,
            rate.Spread,
            rate.EffectiveRate,
            rate.Source.ToString(),
            rate.RecordedAt,
            rate.ValidUntil,
            rate.Convert(100)));
    }
}