// filepath: dotnet/src/ApiPilot.AspNetCore/Diagnostics/ApiPilotRedactionPolicy.cs
// layer: Diagnostics | package: ApiPilot.AspNetCore | since: v0.5.0
// purpose: Named-field redaction rules used by ApiPilotSafeLogger
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class)
//   Depends on : n/a
//   Used by    : ApiPilotSafeLogger
//   See also   : ApiPilotSafeLogger.cs, docs/observability.md (Phase 4.3), CHANGELOG.md (finding A-211)
// -----------------------------------------------------------------------------

namespace ApiPilot.AspNetCore.Diagnostics;

/// <summary>
/// The named-field redaction rules used by <c>ApiPilotSafeLogger</c>.
/// The policy operates on the NAME of a structured log entry or an
/// HTTP header. A value whose name is in the redacted set is replaced
/// with <see cref="RedactedValue"/>. A value whose name is not in the
/// set passes through unchanged.
/// </summary>
/// <remarks>
/// <para>
/// SCOPE. This policy redacts values identified by a known NAME. It
/// does not inspect, rewrite, or scrub opaque string content. An
/// exception message, a scope payload, or any value logged under a
/// generic key (such as <c>Detail</c>) is outside the reach of this
/// policy. Callers must not place secrets in fields whose names are
/// not in the redacted set.
/// </para>
/// <para>
/// The redacted set is fixed by the security contract. It is not
/// overridable at runtime; a wider set belongs to a custom logger
/// implementation.
/// </para>
/// </remarks>
public static class ApiPilotRedactionPolicy
{
    /// <summary>
    /// The literal value written in place of a redacted field.
    /// </summary>
    public const string RedactedValue = "[redacted]";

    private static readonly HashSet<string> s_redactedNames =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Authorization",
            "Cookie",
            "Set-Cookie",
            "X-CSRF-TOKEN",
            "X-XSRF-TOKEN",
            "Proxy-Authorization",
            "WWW-Authenticate",
        };

    /// <summary>
    /// The canonical set of names whose values are redacted. The set
    /// is read-only and the comparison is case-insensitive.
    /// </summary>
    public static IReadOnlySet<string> RedactedNames => s_redactedNames;

    /// <summary>
    /// Redacts the value of a header whose name is in the redacted set.
    /// Returns <see cref="RedactedValue"/> for a matching name; otherwise
    /// returns the value unchanged. A null value becomes an empty string.
    /// </summary>
    /// <param name="headerName">The header name. Compared case-insensitively. Must not be null.</param>
    /// <param name="value">The header value. May be null.</param>
    /// <returns>The redacted or original value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="headerName"/> is null.
    /// </exception>
    public static string RedactHeader(string headerName, string? value)
    {
        ArgumentNullException.ThrowIfNull(headerName);
        if (s_redactedNames.Contains(headerName))
        {
            return RedactedValue;
        }
        return value ?? string.Empty;
    }

    /// <summary>
    /// Redacts a named value whose name is in the redacted set. Behaves
    /// identically to <see cref="RedactHeader"/> but takes an arbitrary
    /// value name rather than an HTTP header name.
    /// </summary>
    /// <param name="valueName">The value name. Compared case-insensitively. Must not be null.</param>
    /// <param name="value">The value. May be null.</param>
    /// <returns>The redacted or original value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="valueName"/> is null.
    /// </exception>
    public static string RedactValue(string valueName, string? value)
    {
        ArgumentNullException.ThrowIfNull(valueName);
        if (s_redactedNames.Contains(valueName))
        {
            return RedactedValue;
        }
        return value ?? string.Empty;
    }
}

