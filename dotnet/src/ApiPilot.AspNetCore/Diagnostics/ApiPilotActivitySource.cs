// filepath: dotnet/src/ApiPilot.AspNetCore/Diagnostics/ApiPilotActivitySource.cs
// layer: Diagnostics | package: ApiPilot.AspNetCore | since: v0.5.0
// purpose: The process-wide ActivitySource ApiPilot uses for tracing
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class)
//   Depends on : System.Diagnostics.ActivitySource
//   Used by    : ApiPilot components that emit activities
//   See also   : docs/observability.md (Phase 4.3), CHANGELOG.md (finding A-212)
// -----------------------------------------------------------------------------
//
// SCOPE
//   The ActivitySource is process-wide and is never disposed. Tags carry
//   only the HTTP method and the error code. Paths, query strings, and
//   exception type names are deliberately not emitted because they can
//   carry sensitive content. A caller that wants richer tags wraps the
//   returned Activity and sets its own tags, having decided they are
//   safe.

using System.Diagnostics;

namespace ApiPilot.AspNetCore.Diagnostics;

/// <summary>
/// The <see cref="ActivitySource"/> ApiPilot uses for tracing. The source
/// is process-wide and is created once at class initialization.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ActivitySource.StartActivity(string, ActivityKind)"/>
/// returns <c>null</c> when no <see cref="ActivityListener"/> is
/// registered. This avoids allocation on the common path where the
/// application has not enabled tracing.
/// </para>
/// <para>
/// The <c>Start*</c> methods set only the HTTP method and the error
/// code. They never set the request path, the query string, or the
/// exception type name, because those values can carry identifiers or
/// application-internal details.
/// </para>
/// <para>
/// The source is not <see cref="IDisposable"/>. It is a process-wide
/// resource and is not disposed by any individual service.
/// </para>
/// </remarks>
public static class ApiPilotActivitySource
{
    /// <summary>The stable name of the ApiPilot activity source.</summary>
    public const string Name = "ApiPilot";

    /// <summary>The version of the ApiPilot activity source.</summary>
    public const string Version = "1.0.0";

    /// <summary>
    /// The process-wide activity source. Created once at class
    /// initialization and never disposed.
    /// </summary>
    public static ActivitySource Source { get; } = new ActivitySource(Name, Version);

    /// <summary>
    /// Starts an activity for CSRF validation. Returns <c>null</c> when no
    /// listener is interested. The only tag set on the activity is
    /// <c>http.method</c>.
    /// </summary>
    /// <param name="httpMethod">The HTTP method. Must not be null.</param>
    /// <returns>The started activity, or <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpMethod"/> is null.
    /// </exception>
    public static Activity? StartCsrfValidation(string httpMethod)
    {
        ArgumentNullException.ThrowIfNull(httpMethod);
        var activity = Source.StartActivity("apipilot.csrf.validation", ActivityKind.Internal);
        activity?.SetTag("http.method", httpMethod);
        return activity;
    }

    /// <summary>
    /// Starts an activity for exception mapping. Returns <c>null</c> when
    /// no listener is interested. The only tag set on the activity is
    /// <c>apipilot.error.code</c>.
    /// </summary>
    /// <param name="errorCode">The mapped error code. Must not be null.</param>
    /// <returns>The started activity, or <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="errorCode"/> is null.
    /// </exception>
    public static Activity? StartExceptionMapping(string errorCode)
    {
        ArgumentNullException.ThrowIfNull(errorCode);
        var activity = Source.StartActivity("apipilot.exception.mapping", ActivityKind.Internal);
        activity?.SetTag("apipilot.error.code", errorCode);
        return activity;
    }
}

