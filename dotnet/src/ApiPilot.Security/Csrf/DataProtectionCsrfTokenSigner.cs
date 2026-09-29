// filepath: dotnet/src/ApiPilot.Security/Csrf/DataProtectionCsrfTokenSigner.cs
// layer: Csrf | package: ApiPilot.Security | since: v0.3.0
// purpose: The default CSRF token signer backed by ASP.NET Core Data Protection
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : ICsrfTokenSigner
//   Depends on : IDataProtectionProvider, CsrfTokenOptions, CsrfTokenFormat,
//                CsrfToken, CsrfTokenParseResult
//   Used by    : CsrfService (Phase 2.2)
//   See also   : ICsrfTokenSigner.cs, CsrfTokenFormat.cs, CsrfTokenOptions.cs
// -----------------------------------------------------------------------------

using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace ApiPilot.Security.Csrf;

/// <summary>
/// The default CSRF token signer. Uses ASP.NET Core Data Protection as
/// the cryptographic authority. The library does not implement its own
/// HMAC, MAC comparison, or key management; it hands a plaintext
/// payload to IDataProtector.Protect and reads the plaintext back with
/// IDataProtector.Unprotect.
/// </summary>
/// <remarks>
/// The wire token is Data Protection's own serialized output. It is
/// opaque to this library. The payload format inside the protection
/// boundary is defined by CsrfTokenFormat.
///
/// On validation, a CryptographicException from Unprotect is mapped to
/// CsrfTokenParseResult.InvalidSignature. This covers tampering, a
/// token from a different key ring, an expired key, and any other
/// integrity failure. The specific cryptographic reason is not
/// distinguished and is never exposed to the client.
///
/// The signer is stateless. It resolves the time from the configured
/// TimeProvider on each call.
/// </remarks>
public sealed class DataProtectionCsrfTokenSigner : ICsrfTokenSigner
{
    private readonly IDataProtector _protector;
    private readonly CsrfTokenOptions _options;

    /// <summary>
    /// Creates a signer that uses the given data protection provider and
    /// options. The provider is asked for a protector with the configured
    /// purpose. All tokens produced by this signer are protected under
    /// that purpose.
    /// </summary>
    /// <param name="provider">
    /// The Data Protection provider. Must not be null.
    /// </param>
    /// <param name="options">
    /// The CSRF token options. Must not be null.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="provider"/> or
    /// <paramref name="options"/> is null.
    /// </exception>
    public DataProtectionCsrfTokenSigner(
        IDataProtectionProvider provider,
        CsrfTokenOptions options)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        _protector = provider.CreateProtector(options.ProtectionPurpose);
    }

    /// <inheritdoc />
    public CsrfToken Sign(string binding)
    {
        ArgumentException.ThrowIfNullOrEmpty(binding);

        var issuedAt = _options.TimeProvider.GetUtcNow();
        var randomBytes = RandomNumberGenerator.GetBytes(_options.TokenEntropyBytes);
        var randomBase64Url = Base64UrlEncode(randomBytes);

        var payload = CsrfTokenFormat.Encode(issuedAt, randomBase64Url, binding);
        var wireValue = _protector.Protect(payload);

        return CsrfToken.From(wireValue);
    }

    /// <inheritdoc />
    public CsrfTokenParseResult TryValidate(CsrfToken token, string binding)
    {
        return ValidateWithMetadata(token, binding).Reason;
    }

    /// <inheritdoc />
    public CsrfTokenValidateOutcome ValidateWithMetadata(CsrfToken token, string binding)
    {
        ArgumentNullException.ThrowIfNull(token);
        ArgumentException.ThrowIfNullOrEmpty(binding);

        string payload;
        try
        {
            payload = _protector.Unprotect(token.Value);
        }
        catch (CryptographicException)
        {
            return CsrfTokenValidateOutcome.Failure(CsrfTokenParseResult.InvalidSignature);
        }

        if (!CsrfTokenFormat.TryDecode(payload, out var issuedAt, out _, out var bindingFingerprint))
        {
            return CsrfTokenValidateOutcome.Failure(CsrfTokenParseResult.Malformed);
        }

        if (!string.Equals(bindingFingerprint, binding, StringComparison.Ordinal))
        {
            return CsrfTokenValidateOutcome.Failure(CsrfTokenParseResult.WrongSession);
        }

        var now = _options.TimeProvider.GetUtcNow();
        var expiresAt = issuedAt.Add(_options.TokenLifetime);
        if (expiresAt <= now)
        {
            return CsrfTokenValidateOutcome.Failure(CsrfTokenParseResult.Expired);
        }

        return CsrfTokenValidateOutcome.Success(issuedAt);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('/', '_')
            .Replace('+', '-');
    }
}

