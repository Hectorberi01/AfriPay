using AfriPay.Application.Common.Errors;
using AfriPay.Application.Disputes.Commands;
using AfriPay.Application.Disputes.Dtos;
using AfriPay.Application.Disputes.Queries;

namespace AfriPay.Application.Disputes;

public interface IDisputeService
{
    Task<Result<DisputeDto>>                    OpenAsync(OpenDisputeCommand cmd,         CancellationToken ct = default);
    Task<Result<DisputeDto>>                    SubmitEvidenceAsync(SubmitEvidenceCommand cmd, CancellationToken ct = default);
    Task<Result<DisputeDto>>                    ResolveAsync(ResolveDisputeCommand cmd,    CancellationToken ct = default);
    Task<Result<DisputeDto>>                    GetAsync(GetDisputeQuery query,            CancellationToken ct = default);
    Task<Result<IReadOnlyList<DisputeDto>>>     ListAsync(ListDisputesQuery query,         CancellationToken ct = default);
}
 
public sealed class DisputeService(
    OpenDisputeHandler     openHandler,
    SubmitEvidenceHandler  evidenceHandler,
    ResolveDisputeHandler  resolveHandler,
    GetDisputeHandler      getHandler,
    ListDisputesHandler    listHandler) : IDisputeService
{
    public Task<Result<DisputeDto>>                OpenAsync(OpenDisputeCommand cmd, CancellationToken ct = default)            => openHandler.HandleAsync(cmd, ct);
    public Task<Result<DisputeDto>>                SubmitEvidenceAsync(SubmitEvidenceCommand cmd, CancellationToken ct = default) => evidenceHandler.HandleAsync(cmd, ct);
    public Task<Result<DisputeDto>>                ResolveAsync(ResolveDisputeCommand cmd, CancellationToken ct = default)       => resolveHandler.HandleAsync(cmd, ct);
    public Task<Result<DisputeDto>>                GetAsync(GetDisputeQuery query, CancellationToken ct = default)               => getHandler.HandleAsync(query, ct);
    public Task<Result<IReadOnlyList<DisputeDto>>> ListAsync(ListDisputesQuery query, CancellationToken ct = default)            => listHandler.HandleAsync(query, ct);
}