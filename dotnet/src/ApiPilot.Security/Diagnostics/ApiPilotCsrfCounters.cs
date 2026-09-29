// filepath: dotnet/src/ApiPilot.Security/Diagnostics/ApiPilotCsrfCounters.cs
// layer: Diagnostics | package: ApiPilot.Security | since: v0.5.0
// purpose: Counters and histograms for the CSRF paths, using System.Diagnostics.Metrics
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : System.IDisposable
//   Depends on : System.Diagnostics.Metrics
//   Used by    : applications that opt in through AddApiPilotCsrfObservability
//   See also   : ApiPilotCsrfObservabilityExtensions.cs, docs/observability.md (Phase 4.3)
// -----------------------------------------------------------------------------
//
// SCOPE
//   The meter name and instrument names are a contract of this
//   version. Applications consume the instruments through the platform
//   MeterListener abstraction. This class owns the Meter and disposes
//   it when the owning scope is disposed.

using System.Diagnostics.Metrics;

namespace ApiPilot.Security.Diagnostics;

/// <summary>
/// The CSRF counters and histograms. The meter is named
/// <c>ApiPilot</c>. Applications consume the instruments through the
/// platform <c>MeterListener</c> abstraction.
/// </summary>
/// <remarks>
/// <para>
/// The instrument names are:
/// <c>apipilot.csrf.issued</c>,
/// <c>apipilot.csrf.validated.ok</c>,
/// <c>apipilot.csrf.validated.failed</c> (with a <c>reason</c> tag),
/// <c>apipilot.csrf.rotated</c>, and
/// <c>apipilot.csrf.validation.duration</c> (unit <c>ms</c>).
/// </para>
/// <para>
/// The <see cref="ValidationFailed(string)"/> method rejects a null,
/// empty, or whitespace reason. It does not validate the reason
/// against an enumeration; the string is passed through as the tag
/// value unchanged.
/// </para>
/// </remarks>
public sealed class ApiPilotCsrfCounters : IDisposable
{
    /// <summary>The stable name of the ApiPilot meter.</summary>
    public const string MeterName = "ApiPilot";

    /// <summary>The version of the ApiPilot meter.</summary>
    public const string MeterVersion = "1.0.0";

    private readonly Meter _meter;
    private readonly Counter<long> _issued;
    private readonly Counter<long> _validatedOk;
    private readonly Counter<long> _validatedFailed;
    private readonly Counter<long> _rotated;
    private readonly Histogram<double> _validationDuration;

    /// <summary>
    /// Creates the counters and the underlying meter.
    /// </summary>
    public ApiPilotCsrfCounters()
    {
        _meter = new Meter(MeterName, MeterVersion);
        _issued = _meter.CreateCounter<long>("apipilot.csrf.issued");
        _validatedOk = _meter.CreateCounter<long>("apipilot.csrf.validated.ok");
        _validatedFailed = _meter.CreateCounter<long>("apipilot.csrf.validated.failed");
        _rotated = _meter.CreateCounter<long>("apipilot.csrf.rotated");
        _validationDuration = _meter.CreateHistogram<double>("apipilot.csrf.validation.duration", "ms");
    }

    /// <summary>
    /// Records a successful token issuance.
    /// </summary>
    public void TokenIssued()
    {
        _issued.Add(1);
    }

    /// <summary>
    /// Records a successful token validation.
    /// </summary>
    public void ValidationSucceeded()
    {
        _validatedOk.Add(1);
    }

    /// <summary>
    /// Records a failed token validation with the internal reason as a
    /// tag value.
    /// </summary>
    /// <param name="reason">
    /// The reason. Must not be null, empty, or whitespace. The string is
    /// passed through as the tag value unchanged.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="reason"/> is null, empty, or
    /// whitespace.
    /// </exception>
    public void ValidationFailed(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        _validatedFailed.Add(1, new KeyValuePair<string, object?>("reason", reason));
    }

    /// <summary>
    /// Records a token rotation.
    /// </summary>
    public void TokenRotated()
    {
        _rotated.Add(1);
    }

    /// <summary>
    /// Records the duration of a validation call.
    /// </summary>
    /// <param name="milliseconds">The duration in milliseconds.</param>
    public void RecordValidationDuration(double milliseconds)
    {
        _validationDuration.Record(milliseconds);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _meter.Dispose();
    }
}

