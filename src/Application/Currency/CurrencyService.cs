using AfriPay.API.Contracts.Requests;
using AfriPay.Application.Common.Errors;
using AfriPay.Application.Currency.Commands;
using AfriPay.Application.Currency.Dtos;
using AfriPay.Application.Currency.Queries;

namespace AfriPay.Application.Currency;

public interface ICurrencyService
{
    Task<Result<ExchangeRateDto>> GetRateAsync(GetExchangeRateQuery query, CancellationToken ct = default);
    Task<Result<ConvertDto>> ConvertAsync(ConvertCurrencyQuery query, CancellationToken ct = default);
}

public sealed class CurrencyService(GetExchangeRateHandler getRateHandler, ConvertCurrencyHandler convertHandler) : ICurrencyService
{
    public Task<Result<ExchangeRateDto>> GetRateAsync(
        GetExchangeRateQuery query, CancellationToken ct = default)
        => getRateHandler.HandleAsync(query, ct);
 
    public Task<Result<ConvertDto>> ConvertAsync(
        ConvertCurrencyQuery query, CancellationToken ct = default)
        => convertHandler.HandleAsync(query, ct);
}