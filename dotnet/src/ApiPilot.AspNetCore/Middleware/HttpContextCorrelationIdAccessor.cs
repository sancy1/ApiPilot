// filepath: dotnet/src/ApiPilot.AspNetCore/Middleware/HttpContextCorrelationIdAccessor.cs
// layer: Middleware | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: ASP.NET Core implementation of ICorrelationIdAccessor reading from HttpContext.Items
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : ICorrelationIdAccessor
//   Depends on : IHttpContextAccessor, ApiPilotCorrelationMiddleware
//   Used by    : application code, ApiPilotExceptionMiddleware
//   See also   : ApiPilotCorrelationMiddleware.cs, ICorrelationIdAccessor.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Metadata;
using Microsoft.AspNetCore.Http;

namespace ApiPilot.AspNetCore.Middleware;

/// <summary>
/// Reads the correlation ID stored on the current HttpContext by
/// <see cref="ApiPilotCorrelationMiddleware"/>. Returns null when there
/// is no current HttpContext or when the middleware did not run for the
/// request.
/// </summary>
public sealed class HttpContextCorrelationIdAccessor : ICorrelationIdAccessor
{
    private readonly IHttpContextAccessor _accessor;

    /// <summary>
    /// Creates the accessor.
    /// </summary>
    /// <param name="accessor">The HTTP context accessor. Must not be null.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="accessor"/> is null.
    /// </exception>
    public HttpContextCorrelationIdAccessor(IHttpContextAccessor accessor)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        _accessor = accessor;
    }

    /// <inheritdoc />
    public string? RequestId
    {
        get
        {
            var httpContext = _accessor.HttpContext;
            if (httpContext is null)
            {
                return null;
            }

            return httpContext.Items[ApiPilotCorrelationMiddleware.CorrelationIdKey] as string;
        }
    }
}

