using AfriPay.Application.Common.Errors;
using AfriPay.Application.Kyb.Commands;
using AfriPay.Application.Kyb.Dtos;
using AfriPay.Application.Kyb.Queries;

namespace AfriPay.Application.Kyb;

public interface IKybService
{
    // Marchand
    Task<Result<KybDto>> GetAsync(GetKybQuery query,                  CancellationToken ct = default);
    Task<Result<KybDto>> InitAsync(InitKybCommand cmd,                CancellationToken ct = default);
    Task<Result<KybDto>> SetBusinessInfoAsync(SetBusinessInfoCommand cmd, CancellationToken ct = default);
    Task<Result<KybDto>> AddDocumentAsync(AddKybDocumentCommand cmd,  CancellationToken ct = default);
    Task<Result<KybDto>> SubmitAsync(SubmitKybCommand cmd,            CancellationToken ct = default);
 
    // Admin
    Task<Result<IReadOnlyList<KybDto>>> ListAsync(ListKybQuery query, CancellationToken ct = default);
    Task<Result<KybDto>> ApproveAsync(ApproveKybCommand cmd,          CancellationToken ct = default);
    Task<Result<KybDto>> RejectAsync(RejectKybCommand cmd,            CancellationToken ct = default);
    Task<Result<KybDto>> RequestAdditionalInfoAsync(RequestAdditionalInfoCommand cmd, CancellationToken ct = default);
}
 
public sealed class KybService(
    GetKybHandler                  getHandler,
    InitKybHandler                 initHandler,
    SetBusinessInfoHandler         setInfoHandler,
    AddKybDocumentHandler          addDocHandler,
    SubmitKybHandler               submitHandler,
    ListKybHandler                 listHandler,
    ApproveKybHandler              approveHandler,
    RejectKybHandler               rejectHandler,
    RequestAdditionalInfoHandler   requestInfoHandler) : IKybService
{
    public Task<Result<KybDto>> GetAsync(GetKybQuery q,                      CancellationToken ct = default) => getHandler.HandleAsync(q, ct);
    public Task<Result<KybDto>> InitAsync(InitKybCommand cmd,                CancellationToken ct = default) => initHandler.HandleAsync(cmd, ct);
    public Task<Result<KybDto>> SetBusinessInfoAsync(SetBusinessInfoCommand cmd, CancellationToken ct = default) => setInfoHandler.HandleAsync(cmd, ct);
    public Task<Result<KybDto>> AddDocumentAsync(AddKybDocumentCommand cmd,  CancellationToken ct = default) => addDocHandler.HandleAsync(cmd, ct);
    public Task<Result<KybDto>> SubmitAsync(SubmitKybCommand cmd,            CancellationToken ct = default) => submitHandler.HandleAsync(cmd, ct);
    public Task<Result<IReadOnlyList<KybDto>>> ListAsync(ListKybQuery q,     CancellationToken ct = default) => listHandler.HandleAsync(q, ct);
    public Task<Result<KybDto>> ApproveAsync(ApproveKybCommand cmd,          CancellationToken ct = default) => approveHandler.HandleAsync(cmd, ct);
    public Task<Result<KybDto>> RejectAsync(RejectKybCommand cmd,            CancellationToken ct = default) => rejectHandler.HandleAsync(cmd, ct);
    public Task<Result<KybDto>> RequestAdditionalInfoAsync(RequestAdditionalInfoCommand cmd, CancellationToken ct = default) => requestInfoHandler.HandleAsync(cmd, ct);
}