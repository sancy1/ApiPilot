// filepath: dotnet/src/ApiPilot.Security/Csrf/CsrfOptions.cs
// layer: Csrf | package: ApiPilot.Security | since: v0.3.0
// purpose: Service-level configuration for the CSRF service and middleware
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed class)
//   Depends on : CsrfTokenParseResult (for the default code mapping)
//   Used by    : CsrfService, CsrfProtectionMiddleware (Phase 2.4)
//   See also   : CsrfTokenOptions.cs, ICsrfService.cs, SPEC.md (CSRF flow)
// -----------------------------------------------------------------------------

namespace ApiPilot.Security.Csrf;

/// <summary>
/// The rotation policy for the CSRF service. The policy decides when a
/// new token is issued for the same binding without an explicit
/// application call to RotateAsync.
/// </summary>
public enum CsrfRotationPolicy
{
    /// <summary>
    /// A new bootstrap request issues a fresh token and records a
    /// rotation marker. This is the default. Every call to the bootstrap
    /// endpoint rotates.
    /// </summary>
    OnBootstrap,

    /// <summary>
    /// A new token is issued only when the previous token is within a
    /// configurable window of expiry. Applications that want to avoid
    /// rotating on every bootstrap call use this policy.
    /// </summary>
    OnWindow,

    /// <summary>
    /// Tokens are never rotated automatically. Only an explicit
    /// RotateAsync call rotates. Applications that control rotation
    /// externally use this policy.
    /// </summary>
    Never,
}

/// <summary>
/// The rotation requirement. Decides whether the host refuses to start
/// when no ICsrfRotationStore is registered.
/// </summary>
public enum CsrfRotationRequirement
{
    /// <summary>
    /// Rotation is optional. When no store is registered, RotateAsync
    /// issues a new token and reports that the previous token remains
    /// valid until natural expiry. This is the default.
    /// </summary>
    Optional,

    /// <summary>
    /// Rotation is required. The host fails at startup when no
    /// ICsrfRotationStore is registered. An application that enables
    /// this setting asserts that token rotation must actually invalidate
    /// the previous token.
    /// </summary>
    Required,
}

/// <summary>
/// Service-level configuration for the CSRF service and middleware. The
/// defaults match the security contract in SPEC.md. Every property is an
/// Option D override point.
/// </summary>
/// <remarks>
/// This type is separate from CsrfTokenOptions. CsrfTokenOptions
/// configures the token signer (purpose, entropy, lifetime, clock).
/// CsrfOptions configures the service and the middleware (header name,
/// rotation policy, bootstrap path, public failure-code mapping, and
/// the pre-auth binding source).
///
/// The CodeMapping dictionary maps internal CsrfTokenParseResult values
/// to the public wire codes the middleware writes. The internal reason
/// is logged and never returned. Ok is not a key in the dictionary;
/// a successful validation produces no code.
///
/// The PreAuthBindingSource delegate is the explicit opt-in for
/// pre-authentication issuance. Without it, an unauthenticated request
/// with no stable subject claim cannot obtain a binding and no token is
/// issued. The fail-closed default is deliberate.
/// </remarks>
public sealed class CsrfOptions
{
    /// <summary>
    /// The header name the middleware reads for the CSRF token. Defaults
    /// to X-CSRF-TOKEN.
    /// </summary>
    public string HeaderName { get; set; } = "X-CSRF-TOKEN";

    /// <summary>
    /// The rotation policy. Defaults to OnBootstrap.
    /// </summary>
    public CsrfRotationPolicy RotationPolicy { get; set; } = CsrfRotationPolicy.OnBootstrap;

    /// <summary>
    /// The rotation requirement. Defaults to Optional. When Required, the
    /// host fails at startup unless an ICsrfRotationStore is registered.
    /// </summary>
    public CsrfRotationRequirement RotationRequirement { get; set; } = CsrfRotationRequirement.Optional;

    /// <summary>
    /// The bootstrap endpoint path. Defaults to /api/csrf. The
    /// middleware exempts this path from CSRF protection so the client
    /// can obtain a token before it has one.
    /// </summary>
    public string BootstrapPath { get; set; } = "/api/csrf";

    /// <summary>
    /// The public wire code for a missing CSRF header. Defaults to
    /// CSRF_HEADER_MISSING. This mapping is separate from CodeMapping
    /// because a missing header is a pre-parse outcome and has no
    /// CsrfTokenParseResult value.
    /// </summary>
    public string MissingHeaderCode { get; set; } = "CSRF_HEADER_MISSING";

    /// <summary>
    /// The mapping from an internal CsrfTokenParseResult value to the
    /// public wire code the middleware writes. The defaults match the
    /// wire contract in SPEC.md. An application may replace or extend the
    /// mapping to suit its clients.
    /// </summary>
    public IDictionary<CsrfTokenParseResult, string> CodeMapping { get; } =
        new Dictionary<CsrfTokenParseResult, string>
        {
            [CsrfTokenParseResult.Malformed] = "CSRF_TOKEN_INVALID",
            [CsrfTokenParseResult.WrongVersion] = "CSRF_TOKEN_INVALID",
            [CsrfTokenParseResult.InvalidSignature] = "CSRF_TOKEN_INVALID",
            [CsrfTokenParseResult.WrongSession] = "CSRF_TOKEN_INVALID",
            [CsrfTokenParseResult.Rotated] = "CSRF_TOKEN_INVALID",
            [CsrfTokenParseResult.Expired] = "CSRF_TOKEN_EXPIRED",
            [CsrfTokenParseResult.BindingMissing] = "CSRF_TOKEN_INVALID",
        };

    /// <summary>
    /// An optional delegate the application sets to enable
    /// pre-authentication issuance. The delegate receives the current
    /// HttpContext and returns a stable binding, or null when it cannot
    /// produce one. When this is null, an unauthenticated request with no
    /// stable subject claim has no binding and cannot receive a token.
    /// </summary>
    public Func<Microsoft.AspNetCore.Http.HttpContext, string?>? PreAuthBindingSource { get; set; }

    /// <summary>
    /// The HTTP methods the middleware protects. Defaults to POST, PUT,
    /// PATCH, and DELETE. Comparison is case-insensitive. Safe methods
    /// (GET, HEAD, OPTIONS, TRACE) are never protected by the global set;
    /// an endpoint that needs protection on a safe method uses the
    /// [ApiPilotRequireCsrf] attribute, which overrides this set.
    /// </summary>
    public ISet<string> ProtectedMethods { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    /// <summary>
    /// Paths exempt from CSRF protection in addition to the bootstrap
    /// path. Defaults to empty. The middleware evaluates the effective
    /// exemption set on each request as ExemptPaths plus the current
    /// CsrfOptions.BootstrapPath. Because the bootstrap path is read at
    /// request time, a later change to BootstrapPath is honored without
    /// re-registering anything.
    /// </summary>
    public IList<Microsoft.AspNetCore.Http.PathString> ExemptPaths { get; } =
        new List<Microsoft.AspNetCore.Http.PathString>();

    /// <summary>
    /// An optional predicate that decides whether a request is exempt
    /// from CSRF protection. When null, only ExemptPaths and the
    /// bootstrap path are exempt. When set, a true return from the
    /// predicate exempts the request regardless of path. The predicate
    /// is evaluated after the path check; a path-exempt request never
    /// invokes the predicate.
    /// </summary>
    public Func<Microsoft.AspNetCore.Http.HttpContext, bool>? ExemptPredicate { get; set; }
}

