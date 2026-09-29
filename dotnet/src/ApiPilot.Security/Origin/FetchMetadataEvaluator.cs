// filepath: dotnet/src/ApiPilot.Security/Origin/FetchMetadataEvaluator.cs
// layer: Origin | package: ApiPilot.Security | since: v0.3.0
// purpose: Evaluate the Sec-Fetch-Site and Sec-Fetch-Mode headers under the configured profile
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class)
//   Depends on : FetchMetadataOptions, FetchMetadataProfile, Microsoft.AspNetCore.Http
//   Used by    : FetchMetadataMiddleware
//   See also   : FetchMetadataOptions.cs, FetchMetadataMiddleware.cs, SPEC.md (Fetch Metadata)
// -----------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;

namespace ApiPilot.Security.Origin;

/// <summary>
/// Evaluates the Fetch Metadata headers under the configured profile.
/// The evaluator is a pure function: it reads the two headers from the
/// request and returns a decision with a reason.
/// </summary>
/// <remarks>
/// AllowMissingHeaders governs the missing-or-malformed case only. It
/// never overrides an explicitly present disallowed value. Under
/// Strict with AllowMissingHeaders=true, a cross-site value is still
/// rejected because cross-site is not in AllowedSiteValues.
///
/// A value not in the spec-defined set (same-origin, same-site,
/// cross-site, none) is treated as malformed and the missing-header
/// policy applies.
/// </remarks>
public static class FetchMetadataEvaluator
{
    /// <summary>
    /// Applies the profile to the current request. Returns true when
    /// the request should proceed.
    /// </summary>
    /// <param name="context">The current HTTP context. Must not be null.</param>
    /// <param name="options">The options. Must not be null.</param>
    /// <param name="reason">
    /// When the method returns false, receives a short reason string
    /// suitable for a log line.
    /// </param>
    /// <returns>True when the request passes the Fetch Metadata policy.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="context"/> or
    /// <paramref name="options"/> is null.
    /// </exception>
    public static bool IsRequestAcceptable(
        HttpContext context,
        FetchMetadataOptions options,
        out string reason)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(options);

        reason = string.Empty;

        if (options.Profile == FetchMetadataProfile.Off)
        {
            return true;
        }

        var siteHeader = context.Request.Headers["Sec-Fetch-Site"].ToString();

        if (string.IsNullOrEmpty(siteHeader) || !IsDefinedSiteValue(siteHeader))
        {
            // Missing or malformed. Apply the missing-header policy.
            if (options.AllowMissingHeaders)
            {
                return true;
            }
            reason = string.IsNullOrEmpty(siteHeader)
                ? "MissingSiteHeader"
                : "MalformedSiteHeader";
            return false;
        }

        // The header is present and well-formed. Apply the allow-list.
        // AllowMissingHeaders is not consulted here.
        if (!options.AllowedSiteValues.Contains(siteHeader))
        {
            reason = "DisallowedSiteValue";
            return false;
        }

        // Check Sec-Fetch-Mode when present.
        var modeHeader = context.Request.Headers["Sec-Fetch-Mode"].ToString();
        if (!string.IsNullOrEmpty(modeHeader))
        {
            if (!options.AllowedModeValues.Contains(modeHeader))
            {
                reason = "DisallowedModeValue";
                return false;
            }
        }

        return true;
    }

    private static bool IsDefinedSiteValue(string value)
    {
        return string.Equals(value, "same-origin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "same-site", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "cross-site", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "none", StringComparison.OrdinalIgnoreCase);
    }
}

