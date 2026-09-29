// filepath: dotnet/src/ApiPilot.AspNetCore/Middleware/CorrelationEnvelope.cs
// layer: Middleware | package: ApiPilot.AspNetCore | since: v0.5.0
// purpose: The single shared seam that resolves the request ID for an error envelope
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class)
//   Depends on : ICorrelationIdAccessor, IOptions<CorrelationOptions>, HttpContext
//   Used by    : ApiPilotExceptionMiddleware, ApiPilotRateLimitRejection
//   See also   : ApiPilotExceptionMiddleware.cs, CHANGELOG.md (A-040, A-118, A-221)
// -----------------------------------------------------------------------------
//
// SCOPE
//   This is the one implementation of the two response-envelope correlation
//   concerns: (1) the request ID is the correlation accessor value when
//   present, falling back to HttpContext.TraceIdentifier; (2) when
//   CorrelationOptions.EchoInResponseBody is false, the request ID in the
//   body is the empty string. Every component that writes an error
//   envelope calls this method so the two concerns cannot drift (the
//   A-040/A-118 defect class).

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.Core.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ApiPilot.AspNetCore.Middleware;

/// <summary>
/// Resolves the request ID that is written into an error envelope,
/// applying the correlation-accessor preference and the
/// <see cref="CorrelationOptions.EchoInResponseBody"/> rule.
/// </summary>
public static class CorrelationEnvelope
{
    /// <summary>
    /// Resolves the request ID for the response body. Returns the
    /// correlation accessor ID when one is available, falling back to
    /// <see cref="HttpContext.TraceIdentifier"/>. Returns the empty string
    /// when <see cref="CorrelationOptions.EchoInResponseBody"/> is false.
    /// </summary>
    /// <param name="httpContext">The current HTTP context. Must not be null.</param>
    /// <param name="correlationAccessor">
    /// The optional correlation accessor. When null, the fallback is used.
    /// </param>
    /// <param name="correlationOptions">
    /// The optional correlation options. When null, EchoInResponseBody is
    /// treated as true.
    /// </param>
    /// <returns>The request ID for the response body.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpContext"/> is null.
    /// </exception>
    public static string ResolveRequestId(
        HttpContext httpContext,
        ICorrelationIdAccessor? correlationAccessor,
        IOptions<CorrelationOptions>? correlationOptions)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var echoInBody = correlationOptions?.Value.EchoInResponseBody ?? true;
        if (!echoInBody)
        {
            return string.Empty;
        }

        var accessorId = correlationAccessor?.RequestId;
        return string.IsNullOrEmpty(accessorId) ? httpContext.TraceIdentifier : accessorId;
    }
}

