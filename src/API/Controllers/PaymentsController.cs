using AfriPay.API.Contracts.Requests;
using AfriPay.API.Contracts.Responses;
using AfriPay.API.Extensions;
using AfriPay.API.Middleware;
using AfriPay.Application.Payments;
using AfriPay.Application.Payments.Dtos;
using AfriPay.Application.Payments.Queries.ListPayments;
using Microsoft.AspNetCore.Mvc;

namespace AfriPay.API.Controllers;

/// <summary>
/// Gestion des paiements AfriPay.
///
/// Endpoints :
///   POST   /v1/payments/initiate        Initier un paiement
///   GET    /v1/payments/{id}            Consulter un paiement
///   DELETE /v1/payments/{id}            Annuler un paiement (pending uniquement)
///   GET    /v1/payments                 Lister les paiements (paginé)
///   GET    /v1/payments/stats           Statistiques agrégées
/// </summary>
[ApiController]
[Route("v1/payments")]
[Produces("application/json")]
public sealed class PaymentsController(IPaymentService paymentService) : ControllerBase
{

    /// <summary>Initie un nouveau paiement.</summary>
    /// <remarks>
    /// Requiert le header <c>Idempotency-Key: {uuid}</c>.
    /// Si la clé a déjà été utilisée dans les dernières 24h, retourne le résultat
    /// original sans appeler le provider à nouveau.
    /// </remarks>
    [HttpPost("initiate")]
    [ProducesResponseType(typeof(PaymentResponse), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 422)]
    [ProducesResponseType(typeof(ErrorResponse), 502)]
    public async Task<IActionResult> Initiate(
        [FromBody]  InitiatePaymentRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken ct)
    {
        var command = new InitiatePaymentDto(
            MerchantId:     HttpContext.GetMerchantId(),
            IdempotencyKey: idempotencyKey,
            Amount:         request.Amount,
            Currency:       request.Currency,
            ProviderKey:    request.Provider,
            PhoneNumber:    request.Customer?.Phone,
            Email:          request.Customer?.Email,
            Metadata:       request.Metadata,
            WebhookUrl:     request.WebhookUrl,
            IsLive:         HttpContext.GetIsLive());

        var result = await paymentService.InitiateAsync(command, ct);

        return result.Match(
            onSuccess: dto    => StatusCode(201, PaymentResponse.From(dto)),
            onError:   error  => error.ToActionResult());
    }


    /// <summary>Retourne le détail d'un paiement.</summary>
    [HttpGet("{paymentId:guid}")]
    [ProducesResponseType(typeof(PaymentResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetById(Guid paymentId, CancellationToken ct)
    {
        var merchantId = HttpContext.GetMerchantId();
        var result = await paymentService.GetByIdAsync(paymentId, merchantId, ct);

        return result.Match(
            onSuccess: dto   => Ok(PaymentResponse.From(dto)),
            onError:   error => error.ToActionResult());
    }


    /// <summary>
    /// Annule un paiement en statut <c>pending</c>.
    /// Retourne 409 si le paiement est dans un état final (completed, failed…).
    /// </summary>
    [HttpDelete("{paymentId:guid}/cancel")]
    [ProducesResponseType(typeof(PaymentResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    public async Task<IActionResult> Cancel(Guid paymentId, CancellationToken ct)
    {
        var merchantId = HttpContext.GetMerchantId();
        var result = await paymentService.CancelAsync(paymentId, merchantId, ct);

        return result.Match(
            onSuccess: dto   => Ok(PaymentResponse.From(dto)),
            onError:   error => error.ToActionResult());
    }


    /// <summary>
    /// Liste paginée des paiements du marchand.
    /// Triée par <c>created_at DESC</c>.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<PaymentResponse>), 200)]
    public async Task<IActionResult> List(
        [FromQuery] string?         status   = null,
        [FromQuery] string?         provider = null,
        [FromQuery] DateTimeOffset? from     = null,
        [FromQuery] DateTimeOffset? to       = null,
        [FromQuery] int             page     = 1,
        [FromQuery] int             pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new ListPaymentsQuery(
            MerchantId: HttpContext.GetMerchantId(),
            Status:     status,
            ProviderKey: provider,
            From:       from,
            To:         to,
            Page:       Math.Clamp(page,     1,   int.MaxValue),
            PageSize:   Math.Clamp(pageSize, 1,   100));

        var result = await paymentService.ListAsync(query, ct);

        return result.Match(
            onSuccess: paged => Ok(new PagedResponse<PaymentResponse>
            {
                Data       = paged.Items.Select(PaymentResponse.From).ToList(),
                TotalCount = paged.TotalCount,
                Page       = paged.Page,
                PageSize   = paged.PageSize,
                HasMore    = paged.HasMore,
            }),
            onError: error => error.ToActionResult());
    }
}