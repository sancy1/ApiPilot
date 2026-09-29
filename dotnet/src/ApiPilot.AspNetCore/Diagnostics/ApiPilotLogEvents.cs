// filepath: dotnet/src/ApiPilot.AspNetCore/Diagnostics/ApiPilotLogEvents.cs
// layer: Diagnostics | package: ApiPilot.AspNetCore | since: v0.5.0
// purpose: Catalogue of the stable event IDs ApiPilot components use for structured logging
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class)
//   Depends on : n/a
//   Used by    : ApiPilot components and applications that key log filters on event IDs
//   See also   : docs/observability.md, CHANGELOG.md (finding A-208)
// -----------------------------------------------------------------------------
//
// INVARIANT
//   Every currently catalogued event ID is unique within this catalogue.
//   Existing IDs are immutable: they are a shipped contract. Applications
//   may key log filters on these values, and renumbering them would break
//   those filters.
//   Future components must allocate unused IDs. The catalogue is not
//   closed; a future phase may add new entries. The invariant does not
//   claim global uniqueness across the entire system, only pairwise
//   distinctness of the values listed here today.

namespace ApiPilot.AspNetCore.Diagnostics;

/// <summary>
/// The stable event IDs ApiPilot uses for structured logging. Each
/// constant names the component that owns the ID and the event it
/// represents. Applications may key log filters on these values.
/// </summary>
/// <remarks>
/// <para>
/// The values are a shipped contract. Once published, they are never
/// renumbered. A future version that adds an event uses the next unused
/// value; it does not shift an existing one.
/// </para>
/// <para>
/// The catalogue is component-scoped. Each component owns a numeric
/// block (1000-1099 for the exception middleware, 2000-2099 for
/// correlation, and so on). A future component allocates from an unused
/// block; it does not reuse a value already catalogued here.
/// </para>
/// </remarks>
public static class ApiPilotLogEvents
{
    /// <summary>Exception mapped to a response at LogLevel.Error.</summary>
    public const int ExceptionMappedError = 1001;

    /// <summary>Exception mapped to a response at LogLevel.Warning.</summary>
    public const int ExceptionMappedWarning = 1002;

    /// <summary>Exception mapped to a response at LogLevel.Information.</summary>
    public const int ExceptionMappedInformation = 1003;

    /// <summary>Exception mapped to a response at LogLevel.Debug.</summary>
    public const int ExceptionMappedDebug = 1004;

    /// <summary>Exception mapped to a response at LogLevel.Critical.</summary>
    public const int ExceptionMappedCritical = 1005;

    /// <summary>Exception mapped to a response at LogLevel.Trace.</summary>
    public const int ExceptionMappedTrace = 1006;

    /// <summary>An exception occurred after the response had already started.</summary>
    public const int ExceptionAfterResponseStarted = 1007;

    /// <summary>An incoming correlation ID failed validation and was replaced.</summary>
    public const int CorrelationInvalidIdWarning = 2001;

    /// <summary>An incoming correlation ID failed validation and the request was rejected.</summary>
    public const int CorrelationInvalidIdRejected = 2002;

    /// <summary>The Accept header did not include an acceptable response media type (406).</summary>
    public const int ContentNegotiationUnacceptableAccept = 3001;

    /// <summary>The Content-Type header on a body-carrying method was not acceptable (415).</summary>
    public const int ContentNegotiationUnsupportedMediaType = 3002;

    /// <summary>The rate-limit rejection adapter rejected a request.</summary>
    public const int RateLimitRejected = 4001;

    /// <summary>The CSRF middleware rejected a request.</summary>
    public const int CsrfRejected = 5001;

    /// <summary>The CSRF transition listener could not resolve the previous binding at logout.</summary>
    public const int CsrfTransitionBindingUnresolved = 6001;

    /// <summary>The CSRF transition listener ran without a registered rotation store.</summary>
    public const int CsrfTransitionNoStore = 6002;

    /// <summary>The Origin policy middleware rejected a request.</summary>
    public const int OriginRejected = 7001;

    /// <summary>The Fetch Metadata middleware rejected a request.</summary>
    public const int FetchMetadataRejected = 8001;

    /// <summary>The security diagnostics emitted a non-fatal startup warning.</summary>
    public const int SecurityDiagnosticsWarning = 9100;
}

