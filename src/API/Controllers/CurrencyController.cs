using AfriPay.API.Contracts.Requests;
using AfriPay.API.Contracts.Responses;
using AfriPay.API.Extensions;
using AfriPay.Application.Common.Errors;
using AfriPay.Application.Currency;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AfriPay.API.Controllers;

/// <summary>
/// Taux de change et conversion de devises.
///
/// Endpoints :
///   GET /v1/currency/rate              Taux de change pour une paire
///   GET /v1/currency/convert           Convertir un montant
/// </summary>
[ApiController]
[Route("v1/currency")]
[Produces("application/json")]
public sealed class CurrencyController(ICurrencyService currency) : ControllerBase
{
    /// <summary>
    /// Retourne le taux de change actuel pour une paire de devises.
    /// </summary>
    /// <remarks>
    /// Exemple : <c>GET /v1/currency/rate?from=EUR&amp;to=XOF</c>
    ///
    /// Le taux effectif est le taux officiel (BCEAO) après déduction du
    /// spread AfriPay (0.5%). C'est ce taux qui est appliqué aux conversions.
    /// </remarks>
    /// <param name="from">Devise source (ISO 4217 : EUR, USD…).</param>
    /// <param name="to">Devise cible (ISO 4217 : XOF, GHS…).</param>
    [HttpGet("rate")]
    [ProducesResponseType(typeof(ExchangeRateResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetRate(
        [FromQuery] string from,
        [FromQuery] string to,
        CancellationToken  ct)
    {
        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
            return AppError
                .Validation("from/to", "Both 'from' and 'to' query parameters are required.")
                .ToActionResult();

        var result = await currency.GetRateAsync(
            new GetExchangeRateQuery(
                From: from.ToUpperInvariant(),
                To:   to.ToUpperInvariant()),
            ct);

        return result.Match(
            onSuccess: dto   => Ok(dto),
            onError:   error => error.ToActionResult());
    }

    /// <summary>
    /// Convertit un montant d'une devise vers une autre.
    /// </summary>
    /// <remarks>
    /// Exemple : <c>GET /v1/currency/convert?amount=100&amp;from=EUR&amp;to=XOF</c>
    ///
    /// Retourne le montant converti au taux effectif (après spread),
    /// ainsi que le montant du spread prélevé par AfriPay.
    /// </remarks>
    /// <param name="amount">Montant en unités minimales (centimes pour EUR).</param>
    /// <param name="from">Devise source.</param>
    /// <param name="to">Devise cible.</param>
    [HttpGet("convert")]
    [ProducesResponseType(typeof(ConvertResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> Convert(
        [FromQuery] long   amount,
        [FromQuery] string from,
        [FromQuery] string to,
        CancellationToken  ct)
    {
        if (amount <= 0)
            return AppError
                .Validation("amount", "Amount must be positive.")
                .ToActionResult();

        var result = await currency.ConvertAsync(
            new ConvertCurrencyQuery(
                Amount: amount,
                From:   from.ToUpperInvariant(),
                To:     to.ToUpperInvariant()),
            ct);

        return result.Match(
            onSuccess: dto   => Ok(dto),
            onError:   error => error.ToActionResult());
    }
}