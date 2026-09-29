// filepath: dotnet/src/ApiPilot.AspNetCore/RateLimiting/RateLimitDiagnosticsHook.cs
// layer: RateLimiting | package: ApiPilot.AspNetCore | since: v0.5.0
// purpose: The platform rate-limiter OnRejected handler that writes the standard rate_limited envelope and diagnostics
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : the RateLimiterOptions.OnRejected delegate
//   Depends on : CorrelationEnvelope, ErrorResponseResult, ApiError, ApiErrorCode,
//                ResponseMetadata, ICorrelationIdAccessor, IOptions<CorrelationOptions>,
//                ApiExceptionOptions, IOptions<ApiExceptionOptions>, ApiPilotLogEvents,
//                RateLimitReasonCodes, System.Diagnostics.Metrics, Microsoft.AspNetCore.RateLimiting
//   Used by    : the application, via options.OnRejected = RateLimitDiagnosticsHook.HandleAsync
//   See also   : ApiPilotRateLimitOptions.cs, docs/rate-limiting.md, CHANGELOG.md (A-235)
// -----------------------------------------------------------------------------
//
// SCOPE
//   This hook integrates with the platform rate limiter. It does NOT
//   implement a limiter, a policy, or a partition. The application owns
//   the limiter configuration; this hook owns only the rejection shape
//   and the diagnostics. The wire error code RATE_LIMITED is fixed by
//   the wire contract; the message and the status are overridable through
//   ApiPilotRateLimitOptions.
//
// STATUS OVERRIDE (A-235)
//   The configured StatusCode is made effective through a per-response
//   clone of ApiExceptionOptions whose ErrorCodeToStatusMap overlays the
//   RATE_LIMITED entry. The clone is local to the request; the registered
//   options instance is never mutated. This keeps ErrorResponseResult as
//   the single serialization seam while honoring the Option D override.
//
// RETRY-AFTER
//   The framework exposes a TimeSpan (delta-seconds) form via
//   MetadataName.RetryAfter and no HTTP-date form. When the lease supplies
//   the metadata, a positive value is emitted as integer delta-seconds
//   (ceiling, invariant culture). A zero or negative value emits nothing.
//   When the lease supplies no metadata, nothing is emitted and no retry
//   interval is calculated.

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.AspNetCore.Diagnostics;
using ApiPilot.AspNetCore.Middleware;
using ApiPilot.AspNetCore.Results;
using ApiPilot.Core.Errors;
using ApiPilot.Core.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Threading.RateLimiting;

namespace ApiPilot.AspNetCore.RateLimiting;

/// <summary>
/// The rate-limiter <c>OnRejected</c> handler. Writes the standard
/// <c>RATE_LIMITED</c> error envelope through <see cref="ErrorResponseResult"/>,
/// emits the <c>apipilot.ratelimit.rejected</c> metric and a sanitized
/// structured log event, and passes through a platform-supplied
/// <c>Retry-After</c> value.
/// </summary>
/// <remarks>
/// <para>
/// The handler resolves the correlation accessor and the correlation
/// options nullably. It falls back to
/// <see cref="HttpContext.TraceIdentifier"/> when no accessor is present,
/// and writes an empty request id when
/// <see cref="CorrelationOptions.EchoInResponseBody"/> is false. These are
/// the same two response-envelope concerns the exception middleware
/// applies, through the shared <see cref="CorrelationEnvelope"/> seam.
/// </para>
/// <para>
/// The handler emits the metric on the <c>ApiPilot</c> meter, named
/// <c>apipilot.ratelimit.rejected</c>. The meter instance is owned by this
/// class and lives for the process; it is not disposed.
/// </para>
/// </remarks>
public static class RateLimitDiagnosticsHook
{
    private static readonly Meter s_meter = new("ApiPilot", "1.0.0");

    private static readonly Counter<long> s_rejected =
        s_meter.CreateCounter<long>("apipilot.ratelimit.rejected");

    private static readonly Action<ILogger, string, string, Exception?> s_logRejected =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(ApiPilotLogEvents.RateLimitRejected, "RateLimitRejected"),
            "Rate-limit rejection: {Code} {Reason}");

    /// <summary>
    /// Handles a rate-limiter rejection. Writes the standard error envelope
    /// and emits the diagnostics. Assign this method to
    /// <c>RateLimiterOptions.OnRejected</c>.
    /// </summary>
    /// <param name="context">The rejection context. Must not be null.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>A task that completes when the response is written.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="context"/> is null.
    /// </exception>
    public static async ValueTask HandleAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var httpContext = context.HttpContext;

        // A client disconnect must not be turned into a server error.
        if (httpContext.RequestAborted.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return;
        }

        // Defensive guard: the limiter runs before the endpoint, so the
        // response normally has not started. If it has, do not write.
        if (httpContext.Response.HasStarted)
        {
            return;
        }

        var services = httpContext.RequestServices;
        var options = services.GetRequiredService<IOptions<ApiPilotRateLimitOptions>>().Value;
        var correlationAccessor = services.GetService<ICorrelationIdAccessor>();
        var correlationOptions = services.GetService<IOptions<CorrelationOptions>>();
        var registeredExceptionOptions = services.GetService<IOptions<ApiExceptionOptions>>()?.Value;

        var reason = ResolveReason(context.Lease);

        s_rejected.Add(1, new KeyValuePair<string, object?>("reason", reason));

        var logger = services.GetService<ILoggerFactory>()?.CreateLogger("ApiPilot.RateLimiting");
        if (logger is not null)
        {
            s_logRejected(logger, ApiErrorCode.RateLimited.Code, reason, null);
        }

        if (options.EmitRetryAfter &&
            context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            if (retryAfter > TimeSpan.Zero)
            {
                var seconds = (long)Math.Ceiling(retryAfter.TotalSeconds);
                httpContext.Response.Headers.RetryAfter =
                    seconds.ToString(CultureInfo.InvariantCulture);
            }
        }

        var requestId = CorrelationEnvelope.ResolveRequestId(
            httpContext,
            correlationAccessor,
            correlationOptions);

        var error = ApiError.Create(ApiErrorCode.RateLimited, options.Message);
        var meta = new ResponseMetadata { RequestId = requestId };
        var errorResponse = new ErrorResponse(error, meta);

        var responseOptions = BuildResponseOptions(registeredExceptionOptions, options.StatusCode);

        await new ErrorResponseResult(errorResponse, responseOptions)
            .ExecuteAsync(httpContext)
            .ConfigureAwait(false);
    }

    private static ApiExceptionOptions BuildResponseOptions(ApiExceptionOptions? source, int statusCode)
    {
        var clone = new ApiExceptionOptions();

        if (source is not null)
        {
            clone.Mappings.Clear();
            foreach (var mapping in source.Mappings)
            {
                clone.Mappings.Add(mapping);
            }

            clone.IncludeExceptionTypeInLogs = source.IncludeExceptionTypeInLogs;
            clone.IncludeExceptionMessageInLogs = source.IncludeExceptionMessageInLogs;
            clone.KnownExceptionLogLevel = source.KnownExceptionLogLevel;
            clone.UnknownExceptionLogLevel = source.UnknownExceptionLogLevel;
            clone.RevealExceptionTypeInResponse = source.RevealExceptionTypeInResponse;
            clone.RevealExceptionMessageInResponse = source.RevealExceptionMessageInResponse;
        }

        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        if (source is not null)
        {
            foreach (var kvp in source.ErrorCodeToStatusMap)
            {
                map[kvp.Key] = kvp.Value;
            }
        }
        map[ApiErrorCode.RateLimited.Code] = statusCode;
        clone.ErrorCodeToStatusMap = map;

        return clone;
    }

    private static string ResolveReason(RateLimitLease lease)
    {
        if (lease.TryGetMetadata(MetadataName.ReasonPhrase, out var reason) &&
            !string.IsNullOrWhiteSpace(reason))
        {
            return reason;
        }
        return RateLimitReasonCodes.None;
    }
}

