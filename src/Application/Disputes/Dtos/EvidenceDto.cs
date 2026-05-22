using AfriPay.Domain.Disputes;

namespace AfriPay.Application.Disputes.Dtos;
public sealed record EvidenceDto(
    string         EvidenceId,
    string         Type,
    string         FileName,
    string?        Description,
    DateTimeOffset SubmittedAt)
{
    public static EvidenceDto FromDomain(DisputeEvidence e) => new(
        e.Id.ToString(), e.Type.ToString().ToLower(),
        e.FileName, e.Description, e.SubmittedAt);
}