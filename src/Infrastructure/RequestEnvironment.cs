using AfriPay.API.Middleware;

namespace AfriPay.Infrastructure;

public interface IRequestEnvironment { bool IsLive { get; } }

public sealed class HttpRequestEnvironment(IHttpContextAccessor accessor) : IRequestEnvironment
{
    public bool IsLive => accessor.HttpContext?.GetIsLive() ?? false;
}