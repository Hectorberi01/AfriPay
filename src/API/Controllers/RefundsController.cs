using AfriPay.API.Contracts.Requests;
using AfriPay.API.Contracts.Responses;
using AfriPay.API.Extensions;
using AfriPay.API.Middleware;
using AfriPay.Application.Refunds;
using Microsoft.AspNetCore.Mvc;

namespace AfriPay.API.Controllers;

/// <summary>
/// Gestion des remboursements.
///
/// Endpoints :
///   POST /v1/payments/{paymentId}/refunds     Créer un remboursement
///   GET  /v1/payments/{paymentId}/refunds     Lister les remboursements d'un paiement
///   GET  /v1/refunds/{refundId}               Consulter un remboursement
/// </summary>
[ApiController]
[Route("v1")]
[Produces("application/json")]
public sealed class RefundsController(IRefundService refundService) : ControllerBase
{
    /// <summary>
    /// Initie un remboursement sur un paiement complété.
    /// </summary>
    /// <remarks>
    /// Règles métier :
    /// - Le paiement doit être en statut <c>completed</c>.
    /// - <c>amount</c> doit être ≤ au montant original.
    /// - La somme de tous les remboursements d'un paiement ne peut pas dépasser son montant.
    /// - Si <c>amount</c> est absent → remboursement total.
    /// </remarks>
    [HttpPost("payments/{paymentId:guid}/refunds")]
    [ProducesResponseType(typeof(RefundResponse), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    [ProducesResponseType(typeof(ErrorResponse), 422)]
    public async Task<IActionResult> Create(
        Guid                 paymentId,
        [FromBody] CreateRefundRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken ct)
    {
        var command = new CreateRefundCommand(
            PaymentId:      paymentId,
            MerchantId:     HttpContext.GetMerchantId(),
            IdempotencyKey: idempotencyKey,
            Amount:         request.Amount,
            Reason:         request.Reason,
            Notes:          request.Notes);

        var result = await refundService.CreateAsync(command, ct);

        return result.Match(
            onSuccess: dto   => StatusCode(201, dto),
            onError:   error => error.ToActionResult());
    }
    

    /// <summary>Retourne tous les remboursements associés à un paiement.</summary>
    [HttpGet("payments/{paymentId:guid}/refunds")]
    [ProducesResponseType(typeof(IReadOnlyList<RefundResponse>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> ListByPayment(Guid paymentId, CancellationToken ct)
    {
        var result = await refundService.ListByPaymentAsync(
            new ListRefundsByPaymentQuery(paymentId, HttpContext.GetMerchantId()),
            ct);

        return result.Match(
            onSuccess: list  => Ok(list),
            onError:   error => error.ToActionResult());
    }


    /// <summary>Retourne le détail d'un remboursement.</summary>
    [HttpGet("refunds/{refundId:guid}")]
    [ProducesResponseType(typeof(RefundResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetById(Guid refundId, CancellationToken ct)
    {
        var result = await refundService.GetByIdAsync(
            new GetRefundQuery(refundId, HttpContext.GetMerchantId()),
            ct);

        return result.Match(
            onSuccess: dto   => Ok(dto),
            onError:   error => error.ToActionResult());
    }
}