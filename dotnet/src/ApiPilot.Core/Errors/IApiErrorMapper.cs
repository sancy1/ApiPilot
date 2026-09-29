// filepath: dotnet/src/ApiPilot.Core/Errors/IApiErrorMapper.cs
// layer: Errors | package: ApiPilot.Core | since: v0.1.0-alpha.0
// purpose: Contract for mapping known exceptions to ApiError values
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (interface)
//   Depends on : ApiError
//   Used by    : ASP.NET Core exception middleware (Phase 1.3)
//   See also   : SPEC.md (error code table), ApiError.cs
// -----------------------------------------------------------------------------

namespace ApiPilot.Core.Errors;

/// <summary>
/// Contract for mapping known exceptions to <see cref="ApiError"/> values.
/// Implementations decide which exception types correspond to which codes
/// and messages. Unrecognized exceptions return false; the caller is then
/// responsible for producing a generic internal error and logging the
/// exception safely.
/// </summary>
/// <remarks>
/// This interface is transport-neutral. It does not see the HTTP request,
/// response, or any ASP.NET Core types. Implementations must never place
/// stack traces, exception type names, or internal details into the
/// mapped error message.
/// </remarks>
public interface IApiErrorMapper
{
    /// <summary>
    /// Attempts to map an exception to an <see cref="ApiError"/>.
    /// </summary>
    /// <param name="exception">The exception to map. Must not be null.</param>
    /// <param name="mapped">
    /// When this method returns true, receives the mapped error. When it
    /// returns false, receives null.
    /// </param>
    /// <returns>
    /// True when the exception is recognized and mapped; false otherwise.
    /// </returns>
    bool TryMap(Exception exception, out ApiError? mapped);
}

