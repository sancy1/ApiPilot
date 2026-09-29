// filepath: dotnet/src/ApiPilot.Security/Csrf/CsrfEndpointMetadata.cs
// layer: Csrf | package: ApiPilot.Security | since: v0.3.0
// purpose: Endpoint metadata that overrides the global CSRF protection policy
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed record)
//   Depends on : n/a
//   Used by    : CsrfAttributes, CsrfMiddleware
//   See also   : CsrfAttributes.cs, CsrfMiddleware.cs
// -----------------------------------------------------------------------------

namespace ApiPilot.Security.Csrf;

/// <summary>
/// The endpoint-level CSRF policy. Set by the [ApiPilotSkipCsrf] and
/// [ApiPilotRequireCsrf] attributes and read by the middleware. The
/// precedence rule is: Require beats Skip beats the global
/// ProtectedMethods set.
/// </summary>
public enum CsrfPolicy
{
    /// <summary>
    /// The endpoint uses the global policy. This is the default when no
    /// attribute is present. A protected method is enforced, a safe
    /// method passes through.
    /// </summary>
    UseGlobal,

    /// <summary>
    /// The endpoint is explicitly protected. Enforced regardless of the
    /// request method, the global ProtectedMethods set, and any
    /// [ApiPilotSkipCsrf] attribute. This makes [ApiPilotRequireCsrf] on
    /// a safe method (for example GET) enforce protection.
    /// </summary>
    Require,

    /// <summary>
    /// The endpoint is explicitly exempt. Bypassed regardless of the
    /// request method and the global ProtectedMethods set. Ignored when
    /// [ApiPilotRequireCsrf] is also present.
    /// </summary>
    Skip,
}

/// <summary>
/// The endpoint metadata attached by the CSRF protection attributes. The
/// middleware reads the metadata from the endpoint and applies the
/// policy. Only one instance is meaningful per endpoint; the last
/// attribute wins when both are present, which is the Require policy.
/// </summary>
/// <param name="Policy">The endpoint CSRF policy.</param>
public sealed record CsrfEndpointMetadata(CsrfPolicy Policy);

