// filepath: dotnet/src/ApiPilot.Security/Origin/OriginValidator.cs
// layer: Origin | package: ApiPilot.Security | since: v0.3.0
// purpose: The RFC 6454 origin comparison used by the Origin policy
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class)
//   Depends on : OriginPolicyOptions, OriginMatchMode, Microsoft.AspNetCore.Http
//   Used by    : OriginMiddleware
//   See also   : OriginPolicyOptions.cs, OriginMiddleware.cs, RFC 6454
// -----------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;

namespace ApiPilot.Security.Origin;

/// <summary>
/// The RFC 6454 origin comparison. Parses an origin string of the
/// form scheme://host[:port] into a normalized tuple and compares
/// two origins by scheme, host, and normalized port.
/// </summary>
/// <remarks>
/// Normalization: scheme and host are lowercased. A missing port is
/// replaced by the scheme default (443 for https, 80 for http). A
/// scheme that has no defined default leaves the port unspecified.
///
/// Malformed origin strings are treated as non-matches. The validator
/// never throws for a malformed input; the caller gets a false
/// result and can treat it as a rejection.
///
/// The validator is not an authorization mechanism and not a CORS
/// implementation. It is a defense-in-depth check for CSRF.
/// </remarks>
public static class OriginValidator
{
    /// <summary>
    /// Attempts to parse an origin string into its normalized parts.
    /// </summary>
    /// <param name="raw">
    /// The origin string. Must not be null. May be empty.
    /// </param>
    /// <param name="parts">
    /// When the method returns true, receives the parsed origin.
    /// </param>
    /// <returns>True when the string is a well-formed origin.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="raw"/> is null.
    /// </exception>
    public static bool TryParseOrigin(string raw, out OriginParts parts)
    {
        ArgumentNullException.ThrowIfNull(raw);
        parts = default;

        if (raw.Length == 0)
        {
            return false;
        }

        // The literal origin "null" is sent by browsers for sandboxed
        // iframes and file:// pages. It is not a usable origin.
        if (string.Equals(raw, "null", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri))
        {
            return false;
        }

        // Uri accepts a path and query. An origin has neither.
        if (!string.IsNullOrEmpty(uri.AbsolutePath) && uri.AbsolutePath != "/")
        {
            return false;
        }
        if (!string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
        {
            return false;
        }

        var scheme = uri.Scheme.ToLowerInvariant();
        var host = uri.Host.ToLowerInvariant();

        if (scheme.Length == 0 || host.Length == 0)
        {
            return false;
        }

        // Uri.Port returns -1 when the port is not explicitly set.
        // Replace with the scheme default when one exists.
        int port;
        if (uri.Port > 0)
        {
            port = uri.Port;
        }
        else
        {
            port = DefaultPortForScheme(scheme);
        }

        parts = new OriginParts(scheme, host, port);
        return true;
    }

    /// <summary>
    /// Compares two origin strings using the given mode. Returns false
    /// when either string is malformed.
    /// </summary>
    /// <param name="requestOrigin">The request origin string.</param>
    /// <param name="allowedOrigin">The allowed origin string.</param>
    /// <param name="mode">The comparison mode.</param>
    /// <returns>True when the origins match under the mode.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when either string is null.
    /// </exception>
    public static bool OriginMatches(
        string requestOrigin,
        string allowedOrigin,
        OriginMatchMode mode)
    {
        ArgumentNullException.ThrowIfNull(requestOrigin);
        ArgumentNullException.ThrowIfNull(allowedOrigin);

        if (!TryParseOrigin(requestOrigin, out var request))
        {
            return false;
        }
        if (!TryParseOrigin(allowedOrigin, out var allowed))
        {
            return false;
        }

        if (!string.Equals(request.Scheme, allowed.Scheme, StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.Equals(request.Host, allowed.Host, StringComparison.Ordinal))
        {
            return false;
        }

        if (mode == OriginMatchMode.Exact)
        {
            return request.Port == allowed.Port;
        }

        if (mode == OriginMatchMode.AnyPortSameHost)
        {
            return true;
        }

        // OriginMatchMode.SameSite is not implemented. The validator
        // returns false rather than approximating registrable-domain
        // matching without a public suffix mechanism.
        return false;
    }

    /// <summary>
    /// Applies the Origin policy to the current request. Returns true
    /// when the request should proceed. Returns false with a reason
    /// string suitable for a log line when the request is rejected.
    /// </summary>
    /// <param name="context">The current HTTP context. Must not be null.</param>
    /// <param name="options">The options. Must not be null.</param>
    /// <param name="reason">
    /// When the method returns false, receives a short reason string.
    /// </param>
    /// <returns>True when the request passes the Origin policy.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="context"/> or
    /// <paramref name="options"/> is null.
    /// </exception>
    public static bool IsRequestOriginAcceptable(
        HttpContext context,
        OriginPolicyOptions options,
        out string reason)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);

        reason = string.Empty;

        var originHeader = context.Request.Headers.Origin.ToString();

        if (string.IsNullOrEmpty(originHeader))
        {
            if (options.AllowRefererFallback)
            {
                var refererHeader = context.Request.Headers.Referer.ToString();
                if (!string.IsNullOrEmpty(refererHeader)
                    && TryParseRefererOrigin(refererHeader, out var refererOrigin))
                {
                    originHeader = refererOrigin;
                }
            }
        }

        if (string.IsNullOrEmpty(originHeader))
        {
            if (!options.AllowMissingOrigin)
            {
                reason = "MissingOrigin";
                return false;
            }
            return true;
        }

        // At this point a present origin is being checked.

        if (options.AllowSameOrigin && IsSameOrigin(context, originHeader))
        {
            return true;
        }

        foreach (var allowed in options.AllowedOrigins)
        {
            if (OriginMatches(originHeader, allowed, options.MatchMode))
            {
                return true;
            }
        }

        reason = "OriginNotAllowed";
        return false;
    }

    private static int DefaultPortForScheme(string scheme)
    {
        return scheme switch
        {
            "https" => 443,
            "http" => 80,
            "wss" => 443,
            "ws" => 80,
            _ => 0,
        };
    }

    private static bool TryParseRefererOrigin(string refererHeader, out string origin)
    {
        origin = string.Empty;
        if (!Uri.TryCreate(refererHeader, UriKind.Absolute, out var uri))
        {
            return false;
        }
        if (uri.Scheme.Length == 0 || uri.Host.Length == 0)
        {
            return false;
        }

        // Build the origin string. Uri.Port is 0 when not set; leave
        // it out so the parse in the comparison step normalizes it.
        var scheme = uri.Scheme.ToLowerInvariant();
        var host = uri.Host.ToLowerInvariant();
        origin = uri.IsDefaultPort
            ? $"{scheme}://{host}"
            : $"{scheme}://{host}:{uri.Port}";
        return true;
    }

    private static bool IsSameOrigin(HttpContext context, string originHeader)
    {
        var request = context.Request;
        var scheme = request.Scheme.ToLowerInvariant();
        var host = request.Host.Host.ToLowerInvariant();
        var port = request.Host.Port ?? DefaultPortForScheme(scheme);

        var own = $"{scheme}://{host}:{port}";
        return OriginMatches(originHeader, own, OriginMatchMode.Exact);
    }
}

/// <summary>
/// The parsed parts of an origin: scheme, host, and normalized port.
/// The port is 0 when the scheme has no defined default.
/// </summary>
/// <param name="Scheme">The lowercased scheme.</param>
/// <param name="Host">The lowercased host.</param>
/// <param name="Port">The normalized port, or 0 for an undefined default.</param>
public readonly record struct OriginParts(string Scheme, string Host, int Port);

