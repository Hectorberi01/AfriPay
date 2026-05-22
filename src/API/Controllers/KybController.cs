using AfriPay.API.Extensions;
using AfriPay.API.Middleware;
using AfriPay.Application.Common.Errors;
using AfriPay.Application.Kyb;
using AfriPay.Application.Kyb.Commands;
using AfriPay.Application.Kyb.Queries;
using AfriPay.Domain.Kyb;
using Microsoft.AspNetCore.Mvc;

namespace AfriPay.API.Controllers;

[ApiController]
[Produces("application/json")]
public sealed class KybController(IKybService kyb) : ControllerBase
{
    [HttpGet("v1/kyb")]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await kyb.GetAsync(
            new GetKybQuery(HttpContext.GetMerchantId()), ct);
 
        return result.Match(
            onSuccess: dto   => Ok(dto),
            onError:   error => error.ToActionResult());
    }
 
    [HttpPost("v1/kyb/init")]
    public async Task<IActionResult> Init(CancellationToken ct)
    {
        var merchant = HttpContext.GetMerchantId();
 
        // Récupérer le pays du marchand depuis le contexte
        // Pour l'instant on le passe en query param
        var country = HttpContext.Request.Query["country"].ToString();
        if (string.IsNullOrWhiteSpace(country))
            return AppError.Validation("country", "Country is required.").ToActionResult();
 
        var result = await kyb.InitAsync(
            new InitKybCommand(merchant, country.ToUpperInvariant()), ct);
 
        return result.Match(
            onSuccess: dto   => StatusCode(201, dto),
            onError:   error => error.ToActionResult());
    }
 
    [HttpPut("v1/kyb/business-info")]
    public async Task<IActionResult> SetBusinessInfo(
        [FromBody] SetBusinessInfoRequest request,
        CancellationToken ct)
    {
        var result = await kyb.SetBusinessInfoAsync(
            new SetBusinessInfoCommand(
                HttpContext.GetMerchantId(),
                request.LegalName,
                request.RegistrationNumber,
                request.TaxId,
                request.BusinessType,
                request.Website,
                request.LegalRepresentativeName,
                request.LegalRepresentativeEmail),
            ct);
 
        return result.Match(
            onSuccess: dto   => Ok(dto),
            onError:   error => error.ToActionResult());
    }
 
    [HttpPost("v1/kyb/documents")]
    public async Task<IActionResult> AddDocument(
        [FromBody] AddDocumentRequest request,
        CancellationToken ct)
    {
        if (!Enum.TryParse<DocumentType>(request.DocumentType, ignoreCase: true, out var docType))
            return AppError.Validation("documentType",
                $"Invalid document type. Valid values: {string.Join(", ", Enum.GetNames<DocumentType>())}.")
                .ToActionResult();
 
        var result = await kyb.AddDocumentAsync(
            new AddKybDocumentCommand(
                HttpContext.GetMerchantId(),
                docType,
                request.FileName,
                request.StorageKey,
                request.ContentType,
                request.FileSizeBytes),
            ct);
 
        return result.Match(
            onSuccess: dto   => StatusCode(201, dto),
            onError:   error => error.ToActionResult());
    }
 
    [HttpPost("v1/kyb/submit")]
    public async Task<IActionResult> Submit(CancellationToken ct)
    {
        var result = await kyb.SubmitAsync(
            new SubmitKybCommand(HttpContext.GetMerchantId()), ct);
 
        return result.Match(
            onSuccess: dto   => Ok(dto),
            onError:   error => error.ToActionResult());
    }
}
 
// Request contracts 
 
public sealed record SetBusinessInfoRequest(
    string  LegalName,
    string? RegistrationNumber,
    string? TaxId,
    string? BusinessType,
    string? Website,
    string? LegalRepresentativeName,
    string? LegalRepresentativeEmail);
 
public sealed record AddDocumentRequest(
    string DocumentType,   // "BusinessRegistration", "OwnerIdentity"…
    string FileName,
    string StorageKey,     // Clé S3/GCS du fichier déjà uploadé
    string ContentType,    // "application/pdf", "image/jpeg"…
    long   FileSizeBytes);
 
public sealed record ReviewRequest(
    string  ReviewerEmail,
    string? Note);