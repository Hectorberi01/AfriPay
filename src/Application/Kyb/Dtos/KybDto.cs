using AfriPay.Domain.Kyb;

namespace AfriPay.Application.Kyb.Dtos;

public sealed record KybDto(
    string          KybId,
    string          MerchantId,
    string          Status,
    string?         LegalName,
    string?         RegistrationNumber,
    string?         TaxId,
    string?         BusinessType,
    string?         Website,
    string?         LegalRepresentativeName,
    string?         LegalRepresentativeEmail,
    string?         ReviewNote,
    bool            LiveAccessAllowed,
    DateTimeOffset  CreatedAt,
    DateTimeOffset? ApprovedAt,
    DateTimeOffset? ExpiresAt,
    IReadOnlyList<KybDocumentDto> Documents)
{
    public static KybDto FromDomain(KybApplication kyb) => new(
        kyb.Id.ToString(),
        kyb.MerchantId.ToString(),
        kyb.Status.ToString().ToLower(),
        kyb.LegalName,
        kyb.RegistrationNumber,
        kyb.TaxId,
        kyb.BusinessType,
        kyb.Website,
        kyb.LegalRepresentativeName,
        kyb.LegalRepresentativeEmail,
        kyb.ReviewNote,
        kyb.LiveAccessAllowed,
        kyb.CreatedAt,
        kyb.ApprovedAt,
        kyb.ExpiresAt,
        kyb.Documents.Select(KybDocumentDto.FromDomain).ToList());
}

public sealed record KybDocumentDto(
    string         DocumentId,
    string         Type,
    string         FileName,
    string         Status,
    string?        ReviewNote,
    DateTimeOffset UploadedAt)
{
    public static KybDocumentDto FromDomain(KybDocument d) => new(
        d.Id.ToString(),
        d.Type.ToString().ToLower(),
        d.FileName,
        d.Status.ToString().ToLower(),
        d.ReviewNote,
        d.UploadedAt);
}