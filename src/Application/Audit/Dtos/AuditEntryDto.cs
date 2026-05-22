using AfriPay.Domain.Audit;

namespace AfriPay.Application.Audit.Dtos;
public sealed record AuditEntryDto(
    string          EntryId,
    string          Action,
    string          EntityType,
    string?         EntityId,
    string?         ActorId,
    string?         ActorType,
    string?         IpAddress,
    string?         Metadata,
    DateTimeOffset  OccurredAt)
{
    public static AuditEntryDto FromDomain(AuditEntry e) => new(
        e.Id.ToString(),
        e.Action.ToString(),
        e.EntityType,
        e.EntityId,
        e.ActorId,
        e.ActorType,
        e.IpAddress,
        e.Metadata,
        e.OccurredAt);
}
