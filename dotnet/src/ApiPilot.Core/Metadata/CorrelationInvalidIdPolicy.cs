// filepath: dotnet/src/ApiPilot.Core/Metadata/CorrelationInvalidIdPolicy.cs
// layer: Metadata | package: ApiPilot.Core | since: v0.2.0-alpha.0
// purpose: Policy for handling an invalid incoming correlation ID
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (enum)
//   Depends on : n/a
//   Used by    : CorrelationOptions, ApiPilotCorrelationMiddleware
//   See also   : CorrelationOptions.cs, SPEC.md (correlation ID rules)
// -----------------------------------------------------------------------------

namespace ApiPilot.Core.Metadata;

/// <summary>
/// The policy applied by the correlation middleware when an incoming
/// correlation ID is present but fails validation. The default is
/// Replace: the invalid value is discarded and a new ID is generated.
/// </summary>
public enum CorrelationInvalidIdPolicy
{
    /// <summary>
    /// Discard the invalid incoming ID and generate a new one. This is
    /// the default and is safe for every deployment.
    /// </summary>
    Replace = 0,

    /// <summary>
    /// Reject the request with a VALIDATION_ERROR response. Use this
    /// when a malformed incoming ID indicates a misconfigured client
    /// and the request should not proceed.
    /// </summary>
    Reject = 1,

    /// <summary>
    /// Use the invalid incoming ID as-is. The middleware logs a
    /// warning. Use this when the application has its own convention
    /// for accepting IDs that do not match the library pattern.
    /// </summary>
    UseAsIs = 2,
}

