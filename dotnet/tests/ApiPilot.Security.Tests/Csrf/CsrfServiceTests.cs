// filepath: dotnet/tests/ApiPilot.Security.Tests/Csrf/CsrfServiceTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Contract tests for the CSRF service composition over signer, binding provider, and rotation store
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class + test doubles)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.Security.Csrf, Microsoft.AspNetCore.Http,
//                Microsoft.Extensions.Logging
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : CsrfService.cs, ICsrfService.cs
// -----------------------------------------------------------------------------

using System.Security.Claims;
using ApiPilot.Security.Csrf;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ApiPilot.Security.Tests.Csrf;

/// <summary>
/// Contract tests for CsrfService. Verifies issuance, validation, and
/// rotation composition over the signer, binding provider, and an optional
/// rotation store. Uses test doubles for the signer and the store so the
/// tests are deterministic.
/// </summary>
[TestClass]
public sealed class CsrfServiceTests
{
    private static DefaultHttpContext AuthenticatedContext(string subject)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("sub", subject),
        }, authenticationType: "Test");
        var ctx = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        return ctx;
    }

    private static DefaultHttpContext AnonymousContext()
    {
        return new DefaultHttpContext();
    }

    private static CsrfService BuildService(
        ICsrfTokenSigner signer,
        CsrfOptions? serviceOptions = null,
        CsrfTokenOptions? tokenOptions = null,
        ICsrfRotationStore? store = null,
        ICsrfBindingProvider? bindingProvider = null)
    {
        var so = serviceOptions ?? new CsrfOptions();
        var to = tokenOptions ?? new CsrfTokenOptions();
        var bp = bindingProvider ?? new CsrfBindingProvider(so);
        var logger = new FakeLogger<CsrfService>();
        return new CsrfService(signer, bp, so, to, logger, store);
    }

    /// <summary>Issue on an authenticated context returns a token.</summary>
    [Test]
    public async Task IssueAsync_Authenticated_ReturnsToken()
    {
        var signer = new FixedSigner();
        var service = BuildService(signer);
        var ctx = AuthenticatedContext("user-42");

        var token = await service.IssueAsync(ctx);

        TestAssert.NotNull(token);
        TestAssert.Equal("fixed-token", token!.Value);
    }

    /// <summary>Issue on an unauthenticated context with no source returns null.</summary>
    [Test]
    public async Task IssueAsync_Anonymous_NoSource_ReturnsNull()
    {
        var signer = new FixedSigner();
        var service = BuildService(signer);
        var ctx = AnonymousContext();

        var token = await service.IssueAsync(ctx);

        TestAssert.Null(token);
    }

    /// <summary>Issue on an anonymous context with a pre-auth source returns a token.</summary>
    [Test]
    public async Task IssueAsync_AnonymousWithPreAuthSource_ReturnsToken()
    {
        var signer = new FixedSigner();
        var opts = new CsrfOptions { PreAuthBindingSource = _ => "preauth-42" };
        var service = BuildService(signer, serviceOptions: opts);
        var ctx = AnonymousContext();

        var token = await service.IssueAsync(ctx);

        TestAssert.NotNull(token);
    }

    /// <summary>Validate with no header returns the MissingHeaderCode.</summary>
    [Test]
    public async Task ValidateAsync_NoHeader_ReturnsMissingHeaderCode()
    {
        var signer = new FixedSigner();
        var service = BuildService(signer);
        var ctx = AuthenticatedContext("user-42");

        var result = await service.ValidateAsync(ctx, rawHeaderValue: null);

        TestAssert.False(result.IsSuccess);
        TestAssert.Equal("CSRF_HEADER_MISSING", result.PublicCode);
    }

    /// <summary>Validate with a custom MissingHeaderCode honors the override.</summary>
    [Test]
    public async Task ValidateAsync_NoHeader_HonorsCustomMissingHeaderCode()
    {
        var signer = new FixedSigner();
        var opts = new CsrfOptions { MissingHeaderCode = "CUSTOM_MISSING" };
        var service = BuildService(signer, serviceOptions: opts);
        var ctx = AuthenticatedContext("user-42");

        var result = await service.ValidateAsync(ctx, rawHeaderValue: null);

        TestAssert.Equal("CUSTOM_MISSING", result.PublicCode);
    }

    /// <summary>Validate with a valid token returns Success.</summary>
    [Test]
    public async Task ValidateAsync_ValidToken_ReturnsSuccess()
    {
        var signer = new FixedSigner { ValidationResult = CsrfTokenParseResult.Ok };
        var service = BuildService(signer);
        var ctx = AuthenticatedContext("user-42");

        var result = await service.ValidateAsync(ctx, "any-token-value");

        TestAssert.True(result.IsSuccess);
        TestAssert.Equal(CsrfTokenParseResult.Ok, result.Reason);
    }

    /// <summary>Validate with a tampered token maps to CSRF_TOKEN_INVALID.</summary>
    [Test]
    public async Task ValidateAsync_InvalidSignature_MapsToCsrfTokenInvalid()
    {
        var signer = new FixedSigner { ValidationResult = CsrfTokenParseResult.InvalidSignature };
        var service = BuildService(signer);
        var ctx = AuthenticatedContext("user-42");

        var result = await service.ValidateAsync(ctx, "any-token-value");

        TestAssert.Equal(CsrfTokenParseResult.InvalidSignature, result.Reason);
        TestAssert.Equal("CSRF_TOKEN_INVALID", result.PublicCode);
    }

    /// <summary>Validate with an expired token maps to CSRF_TOKEN_EXPIRED.</summary>
    [Test]
    public async Task ValidateAsync_Expired_MapsToCsrfTokenExpired()
    {
        var signer = new FixedSigner { ValidationResult = CsrfTokenParseResult.Expired };
        var service = BuildService(signer);
        var ctx = AuthenticatedContext("user-42");

        var result = await service.ValidateAsync(ctx, "any-token-value");

        TestAssert.Equal(CsrfTokenParseResult.Expired, result.Reason);
        TestAssert.Equal("CSRF_TOKEN_EXPIRED", result.PublicCode);
    }

    /// <summary>Validate on an anonymous context with no binding fails with BindingMissing.</summary>
    [Test]
    public async Task ValidateAsync_NoBinding_ReturnsBindingMissing()
    {
        var signer = new FixedSigner();
        var service = BuildService(signer);
        var ctx = AnonymousContext();

        var result = await service.ValidateAsync(ctx, "any-token-value");

        TestAssert.Equal(CsrfTokenParseResult.BindingMissing, result.Reason);
        TestAssert.Equal("CSRF_TOKEN_INVALID", result.PublicCode);
    }

    /// <summary>A custom CodeMapping is honored on failure.</summary>
    [Test]
    public async Task ValidateAsync_CustomCodeMapping_HonorsOverride()
    {
        var signer = new FixedSigner { ValidationResult = CsrfTokenParseResult.Expired };
        var opts = new CsrfOptions();
        opts.CodeMapping[CsrfTokenParseResult.Expired] = "CUSTOM_EXPIRED";
        var service = BuildService(signer, serviceOptions: opts);
        var ctx = AuthenticatedContext("user-42");

        var result = await service.ValidateAsync(ctx, "any-token-value");

        TestAssert.Equal("CUSTOM_EXPIRED", result.PublicCode);
    }

    /// <summary>Rotate on an authenticated context returns a new token.</summary>
    [Test]
    public async Task RotateAsync_Authenticated_ReturnsToken()
    {
        var signer = new FixedSigner();
        var service = BuildService(signer);
        var ctx = AuthenticatedContext("user-42");

        var token = await service.RotateAsync(ctx);

        TestAssert.NotNull(token);
    }

    /// <summary>Rotate on an anonymous context with no binding returns null.</summary>
    [Test]
    public async Task RotateAsync_Anonymous_ReturnsNull()
    {
        var signer = new FixedSigner();
        var service = BuildService(signer);
        var ctx = AnonymousContext();

        var token = await service.RotateAsync(ctx);

        TestAssert.Null(token);
    }

    /// <summary>Rotate without a store returns a token.</summary>
    [Test]
    public async Task RotateAsync_NoStore_ReturnsToken()
    {
        var signer = new FixedSigner();
        var service = BuildService(signer);
        var ctx = AuthenticatedContext("user-42");

        var token = await service.RotateAsync(ctx);

        TestAssert.NotNull(token);
    }

    /// <summary>Rotate with a store writes a marker.</summary>
    [Test]
    public async Task RotateAsync_WithStore_WritesMarker()
    {
        var signer = new FixedSigner();
        var store = new InMemoryRotationStore();
        var service = BuildService(signer, store: store);
        var ctx = AuthenticatedContext("user-42");

        await service.RotateAsync(ctx);

        TestAssert.True(store.SetCalls >= 1);
    }

    /// <summary>
    /// Validate with a store and a marker newer than the token rejects
    /// the token as Rotated.
    /// </summary>
    [Test]
    public async Task ValidateAsync_StoreWithNewerMarker_ReturnsRotated()
    {
        var signer = new FixedSigner
        {
            ValidationResult = CsrfTokenParseResult.Ok,
            IssueTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        };
        var store = new InMemoryRotationStore
        {
            Marker = new DateTimeOffset(2026, 1, 1, 1, 0, 0, TimeSpan.Zero),
        };
        var service = BuildService(signer, store: store);
        var ctx = AuthenticatedContext("user-42");

        var result = await service.ValidateAsync(ctx, "any-token-value");

        TestAssert.Equal(CsrfTokenParseResult.Rotated, result.Reason);
        TestAssert.Equal("CSRF_TOKEN_INVALID", result.PublicCode);
    }

    /// <summary>
    /// Validate with a store and a marker older than the token accepts
    /// the token.
    /// </summary>
    [Test]
    public async Task ValidateAsync_StoreWithOlderMarker_ReturnsSuccess()
    {
        var signer = new FixedSigner
        {
            ValidationResult = CsrfTokenParseResult.Ok,
            IssueTime = new DateTimeOffset(2026, 1, 1, 1, 0, 0, TimeSpan.Zero),
        };
        var store = new InMemoryRotationStore
        {
            Marker = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        };
        var service = BuildService(signer, store: store);
        var ctx = AuthenticatedContext("user-42");

        var result = await service.ValidateAsync(ctx, "any-token-value");

        TestAssert.True(result.IsSuccess);
    }

    /// <summary>A null context is rejected by IssueAsync.</summary>
    [Test]
    public async Task IssueAsync_NullContext_Throws()
    {
        var signer = new FixedSigner();
        var service = BuildService(signer);
        try
        {
            await service.IssueAsync(null!);
            TestAssert.True(false, "Expected ArgumentNullException.");
        }
        catch (ArgumentNullException)
        {
            // Expected.
        }
    }

    /// <summary>
    /// RotateAsync without a store logs a warning on every call, not
    /// only the first. This matches the Q2 design decision: no silent
    /// no-op, no throttling, honest operational state on every call.
    /// </summary>
    [Test]
    public async Task RotateAsync_NoStore_LogsWarningOnEveryCall()
    {
        var signer = new FixedSigner();
        var serviceOptions = new CsrfOptions();
        var tokenOptions = new CsrfTokenOptions();
        var bindingProvider = new CsrfBindingProvider(serviceOptions);
        var logger = new FakeLogger<CsrfService>();
        var service = new CsrfService(signer, bindingProvider, serviceOptions, tokenOptions, logger);
        var ctx = AuthenticatedContext("user-42");

        await service.RotateAsync(ctx);
        await service.RotateAsync(ctx);

        var warningCount = 0;
        foreach (var entry in logger.Entries)
        {
            if (entry.Level == LogLevel.Warning) { warningCount++; }
        }
        TestAssert.Equal(2, warningCount);
    }

    /// <summary>A null signer is rejected by the constructor.</summary>
    [Test]
    public void Constructor_RejectsNullSigner()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            new CsrfService(null!, new CsrfBindingProvider(new CsrfOptions()),
                new CsrfOptions(), new CsrfTokenOptions(),
                new FakeLogger<CsrfService>()));
    }
}

/// <summary>A deterministic ICsrfTokenSigner for tests.</summary>
internal sealed class FixedSigner : ICsrfTokenSigner
{
    public CsrfTokenParseResult ValidationResult { get; set; } = CsrfTokenParseResult.Ok;

    public DateTimeOffset IssueTime { get; set; } = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public CsrfToken Sign(string binding) => CsrfToken.From("fixed-token");

    public CsrfTokenParseResult TryValidate(CsrfToken token, string binding)
        => ValidateWithMetadata(token, binding).Reason;

    public CsrfTokenValidateOutcome ValidateWithMetadata(CsrfToken token, string binding)
    {
        return ValidationResult == CsrfTokenParseResult.Ok
            ? CsrfTokenValidateOutcome.Success(IssueTime)
            : CsrfTokenValidateOutcome.Failure(ValidationResult);
    }
}

/// <summary>An in-memory ICsrfRotationStore for tests.</summary>
internal sealed class InMemoryRotationStore : ICsrfRotationStore
{
    public DateTimeOffset? Marker { get; set; }

    public int SetCalls { get; private set; }

    public Task<DateTimeOffset?> GetRotationMarkerAsync(
        string binding,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Marker);
    }

    public Task SetRotationMarkerAsync(
        string binding,
        DateTimeOffset rotatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        Marker = rotatedAtUtc;
        SetCalls++;
        return Task.CompletedTask;
    }

    public Task ClearRotationMarkerAsync(
        string binding,
        CancellationToken cancellationToken = default)
    {
        Marker = null;
        return Task.CompletedTask;
    }
}

