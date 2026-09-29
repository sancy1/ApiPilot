// filepath: dotnet/src/ApiPilot.Core/Configuration/ContentNegotiationOptions.cs
// layer: Configuration | package: ApiPilot.Core | since: v0.2.0-alpha.0
// purpose: Mutable configuration for content negotiation policy
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed class)
//   Depends on : n/a
//   Used by    : ASP.NET Core content negotiation middleware (Phase 1.7)
//   See also   : SPEC.md (content negotiation), ApiPilotContentNegotiationMiddleware.cs
// -----------------------------------------------------------------------------

namespace ApiPilot.Core.Configuration;

/// <summary>
/// Mutable configuration for the ApiPilot content negotiation middleware.
/// The defaults match the content negotiation section of SPEC.md: JSON only,
/// the full wildcard accepted, missing Accept header acceptable, missing
/// Content-Type on body-carrying methods rejected.
/// </summary>
/// <remarks>
/// The Accept header check only recognizes the full wildcard */*. Type-level
/// wildcards such as application/* are not supported. Applications that need
/// them configure AcceptableResponseMediaTypes to include the specific media
/// types their clients send, or replace the middleware. This simplification
/// matches the "presence, not preference" approach: the check decides whether
/// a request is acceptable, not which of several acceptable types the client
/// would prefer.
/// </remarks>
public sealed class ContentNegotiationOptions
{
    /// <summary>
    /// The set of media types the middleware will serve in a response. Defaults
    /// to a single entry, application/json. Comparison is case-insensitive.
    /// </summary>
    public ISet<string> AcceptableResponseMediaTypes { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "application/json" };

    /// <summary>
    /// Whether the full wildcard */* in the Accept header is treated as
    /// accepting any media type. Defaults to true, which matches the RFC 7231
    /// convention that a client sending */* accepts anything the server can
    /// produce.
    /// </summary>
    public bool AcceptWildcard { get; set; } = true;

    /// <summary>
    /// The set of media types the middleware will accept in a request Content-Type
    /// header on body-carrying methods. Defaults to a single entry, application/json.
    /// Comparison is case-insensitive.
    /// </summary>
    public ISet<string> AcceptableRequestMediaTypes { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "application/json" };

    /// <summary>
    /// Whether a request on a body-carrying method with no Content-Type header
    /// is accepted. Defaults to false, which rejects such requests with HTTP 415.
    /// Applications that accept requests without a Content-Type set this to true.
    /// </summary>
    public bool AcceptMissingContentType { get; set; }

    /// <summary>
    /// The HTTP methods that are considered to carry a body and therefore require
    /// a Content-Type header. Defaults to POST, PUT, and PATCH. Comparison is
    /// case-insensitive. Applications that want DELETE (or other methods) treated
    /// as body-carrying add them here.
    /// </summary>
    public ISet<string> BodyCarryingMethods { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH" };
}

