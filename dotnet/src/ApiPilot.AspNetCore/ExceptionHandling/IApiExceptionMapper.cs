// filepath: dotnet/src/ApiPilot.AspNetCore/ExceptionHandling/IApiExceptionMapper.cs
// layer: ExceptionHandling | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: ASP.NET Core adapter contract for mapping exceptions to ApiError values
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (interface)
//   Depends on : ApiPilot.Core.Errors.ApiError, Microsoft.AspNetCore.Http.HttpContext
//   Used by    : DefaultApiExceptionMapper, ApiPilotExceptionMiddleware
//   See also   : ApiPilot.Core.Errors.IApiErrorMapper (Core contract), SPEC.md
// -----------------------------------------------------------------------------

using ApiPilot.Core.Errors;
using Microsoft.AspNetCore.Http;

namespace ApiPilot.AspNetCore.ExceptionHandling;

/// <summary>
/// Maps exceptions to <see cref="ApiError"/> values in the ASP.NET Core
/// pipeline. Unlike the transport-neutral <see cref="IApiErrorMapper"/> from
/// ApiPilot.Core, this interface receives the current <see cref="HttpContext"/>
/// so implementations can consider request-specific context. Every exception
/// must produce an error; unknown exceptions fall back to a generic internal
/// error with a safe message.
/// </summary>
public interface IApiExceptionMapper
{
    /// <summary>
    /// Maps the given exception to an <see cref="ApiError"/>. The returned
    /// error must carry a stable code and a message that never contains
    /// stack traces, cryptographic details, or other internal information.
    /// </summary>
    /// <param name="exception">The exception to map. Must not be null.</param>
    /// <param name="httpContext">The current HTTP context. Must not be null.</param>
    /// <returns>An ApiError describing the failure safely.</returns>
    ApiError Map(Exception exception, HttpContext httpContext);
}

