// filepath: dotnet/src/ApiPilot.Security/Csrf/CsrfTokenFormat.cs
// layer: Csrf | package: ApiPilot.Security | since: v0.3.0
// purpose: Encode and decode the plaintext payload that Data Protection protects
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class)
//   Depends on : n/a (BCL only)
//   Used by    : DataProtectionCsrfTokenSigner
//   See also   : ICsrfTokenSigner.cs, CsrfTokenParseResult.cs
// -----------------------------------------------------------------------------

using System.Globalization;

namespace ApiPilot.Security.Csrf;

/// <summary>
/// The plaintext payload format for a CSRF token. The payload is the
/// value handed to Data Protection for signing. It is a three-part
/// pipe-delimited string:
///
///     issuedAtUnixSeconds | randomBase64Url | bindingFingerprint
///
/// The payload format is independent of the wire format. The wire token
/// is whatever Data Protection produces from this payload. The wire
/// format is opaque and is never parsed by this library.
/// </summary>
/// <remarks>
/// The codec is a pure function. It performs no cryptography. Data
/// Protection is the cryptographic authority; this type only prepares
/// and interprets the bytes that Data Protection protects and
/// unprotects.
///
/// The delimiter is a pipe character. The three parts never contain a
/// pipe: the issued-at is a decimal integer, the random value is a
/// base64url-encoded string (alphabet A-Z, a-z, 0-9, -, _), and the
/// binding fingerprint is produced by the caller and is expected to be
/// free of pipes. If a caller supplies a binding that contains a pipe,
/// Encode throws ArgumentException.
/// </remarks>
public static class CsrfTokenFormat
{
    /// <summary>
    /// The delimiter used between the three payload parts. The value is
    /// a single pipe character.
    /// </summary>
    public const char Delimiter = '|';

    /// <summary>
    /// Encodes the three payload parts into the plaintext string that
    /// Data Protection will protect. The issuedAt value is written as a
    /// decimal integer in the invariant culture.
    /// </summary>
    /// <param name="issuedAt">
    /// The moment the token is issued, expressed as a DateTimeOffset in
    /// UTC. Must be a UTC value.
    /// </param>
    /// <param name="randomBase64Url">
    /// The base64url-encoded random value. Must be non-null and must not
    /// contain the pipe delimiter.
    /// </param>
    /// <param name="bindingFingerprint">
    /// The binding fingerprint. Must be non-null and must not contain the
    /// pipe delimiter.
    /// </param>
    /// <returns>The plaintext payload.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when a part is null, empty, or contains the pipe delimiter.
    /// </exception>
    public static string Encode(
        DateTimeOffset issuedAt,
        string randomBase64Url,
        string bindingFingerprint)
    {
        if (issuedAt.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                "issuedAt must be a UTC DateTimeOffset (offset 0).", nameof(issuedAt));
        }
        ArgumentException.ThrowIfNullOrEmpty(randomBase64Url);
        ArgumentException.ThrowIfNullOrEmpty(bindingFingerprint);
        if (randomBase64Url.Contains(Delimiter))
        {
            throw new ArgumentException(
                "randomBase64Url must not contain the pipe delimiter.", nameof(randomBase64Url));
        }
        if (bindingFingerprint.Contains(Delimiter))
        {
            throw new ArgumentException(
                "bindingFingerprint must not contain the pipe delimiter.", nameof(bindingFingerprint));
        }

        var issuedAtSeconds = issuedAt.ToUnixTimeSeconds();
        return string.Concat(
            issuedAtSeconds.ToString(CultureInfo.InvariantCulture),
            Delimiter,
            randomBase64Url,
            Delimiter,
            bindingFingerprint);
    }

    /// <summary>
    /// Attempts to decode a plaintext payload produced by
    /// <see cref="Encode"/>. Returns true when the payload is
    /// structurally valid and every part can be parsed. Returns false
    /// otherwise; the caller maps a false return to
    /// <see cref="CsrfTokenParseResult.Malformed"/> or to a more specific
    /// reason after further checks.
    /// </summary>
    /// <param name="payload">The plaintext payload.</param>
    /// <param name="issuedAt">
    /// When the method returns true, receives the token issue time.
    /// </param>
    /// <param name="randomBase64Url">
    /// When the method returns true, receives the base64url random value.
    /// </param>
    /// <param name="bindingFingerprint">
    /// When the method returns true, receives the binding fingerprint.
    /// </param>
    /// <returns>True when the payload is structurally valid.</returns>
    public static bool TryDecode(
        string payload,
        out DateTimeOffset issuedAt,
        out string randomBase64Url,
        out string bindingFingerprint)
    {
        issuedAt = default;
        randomBase64Url = string.Empty;
        bindingFingerprint = string.Empty;

        if (string.IsNullOrEmpty(payload))
        {
            return false;
        }

        var first = payload.IndexOf(Delimiter);
        if (first <= 0)
        {
            return false;
        }

        var second = payload.IndexOf(Delimiter, first + 1);
        if (second <= first + 1)
        {
            return false;
        }

        // The binding may itself not contain a pipe, so there must be no
        // third delimiter beyond the second.
        if (payload.IndexOf(Delimiter, second + 1) >= 0)
        {
            return false;
        }

        var issuedPart = payload.Substring(0, first);
        var randomPart = payload.Substring(first + 1, second - first - 1);
        var bindingPart = payload.Substring(second + 1);

        if (randomPart.Length == 0 || bindingPart.Length == 0)
        {
            return false;
        }

        if (!long.TryParse(issuedPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var issuedSeconds))
        {
            return false;
        }

        // Reject seconds that cannot be represented as a DateTimeOffset.
        if (issuedSeconds < -62135596800 || issuedSeconds > 253402300799)
        {
            return false;
        }

        issuedAt = DateTimeOffset.FromUnixTimeSeconds(issuedSeconds);
        randomBase64Url = randomPart;
        bindingFingerprint = bindingPart;
        return true;
    }
}

