// filepath: dotnet/src/ApiPilot.Core/Metadata/CorrelationOptions.cs
// layer: Metadata | package: ApiPilot.Core | since: v0.1.0-alpha.0
// purpose: Mutable configuration for correlation ID handling
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed class)
//   Depends on : CorrelationInvalidIdPolicy
//   Used by    : ASP.NET Core correlation middleware (Phase 1.6)
//   See also   : CorrelationIdValidator.cs, ICorrelationIdGenerator.cs, SPEC.md
// -----------------------------------------------------------------------------

namespace ApiPilot.Core.Metadata;

/// <summary>
/// Mutable configuration for correlation ID handling. Intended for binding
/// from application configuration. The default values reflect the ApiPilot
/// correlation contract documented in SPEC.md.
/// </summary>
public sealed class CorrelationOptions
{
    /// <summary>
    /// The header name read from incoming requests and echoed on responses.
    /// Defaults to X-Request-Id.
    /// </summary>
    public string HeaderName { get; set; } = "X-Request-Id";

    /// <summary>
    /// Whether the correlation ID is included in the response envelope
    /// meta.requestId. Defaults to true.
    /// </summary>
    public bool EchoInResponseBody { get; set; } = true;

    /// <summary>
    /// Whether the correlation ID is echoed in a response header. Defaults
    /// to true.
    /// </summary>
    public bool EchoInResponseHeader { get; set; } = true;

    /// <summary>
    /// Whether incoming correlation IDs are validated against
    /// <see cref="ValidationPattern"/>. When false, any non-null incoming
    /// value is used. Defaults to true.
    /// </summary>
    public bool ValidateIncoming { get; set; } = true;

    /// <summary>
    /// The regular expression pattern applied to incoming correlation IDs
    /// when validation is enabled. Defaults to 8 to 128 alphanumeric,
    /// underscore, or hyphen characters.
    /// </summary>
    public string ValidationPattern { get; set; } = "^[A-Za-z0-9_-]{8,128}$";

    /// <summary>
    /// The policy applied when an incoming correlation ID is present but
    /// fails validation. Defaults to Replace, which discards the invalid
    /// value and generates a new one.
    /// </summary>
    public CorrelationInvalidIdPolicy InvalidIncomingIdPolicy { get; set; }
}

