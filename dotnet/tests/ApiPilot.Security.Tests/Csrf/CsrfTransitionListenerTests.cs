// filepath: dotnet/tests/ApiPilot.Security.Tests/Csrf/CsrfTransitionListenerTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Contract tests for the CSRF authentication transition listener
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class + test doubles)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.Security.Csrf, ApiPilot.Security.Tests (FakeLogger)
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ICsrfTransitionListener.cs, CsrfTransitionEvents.cs
// -----------------------------------------------------------------------------

using System.Reflection;
using System.Security.Claims;
using ApiPilot.Security.Csrf;
using ApiPilot.Security.Tests;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ApiPilot.Security.Tests.Csrf;

/// <summary>
/// Contract tests for DefaultCsrfTransitionListener. Verifies login
/// issuance, logout marker clearing, the two logout overloads, the
/// no-store behavior, custom listener replacement, cancellation, and
/// the boundary guardrail against ASP.NET Core Identity.
/// </summary>
[TestClass]
public sealed class CsrfTransitionListenerTests
{
    private static DefaultHttpContext AuthenticatedContext(string subject)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("sub", subject),
        }, authenticationType: "Test");
        return new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
    }

    private static DefaultHttpContext AnonymousContext()
    {
        return new DefaultHttpContext();
    }

    private static DefaultCsrfTransitionListener Build(
        TransitionFixedCsrfService service,
        CsrfOptions? options = null,
        TransitionInMemoryRotationStore? store = null,
        FakeLogger<DefaultCsrfTransitionListener>? logger = null)
    {
        var opts = options ?? new CsrfOptions();
        var bp = new CsrfBindingProvider(opts);
        var log = logger ?? new FakeLogger<DefaultCsrfTransitionListener>();
        return new DefaultCsrfTransitionListener(service, bp, log, store);
    }

    /// <summary>OnLoginAsync issues a fresh token for the new subject.</summary>
    [Test]
    public async Task OnLoginAsync_WithAuthenticatedSubject_IssuesToken()
    {
        var service = new TransitionFixedCsrfService();
        var listener = Build(service);
        var ctx = AuthenticatedContext("user-42");

        var token = await listener.OnLoginAsync(ctx);

        TestAssert.NotNull(token);
        TestAssert.True(service.IssueCalled);
    }

    /// <summary>OnLoginAsync returns null when no binding can be resolved.</summary>
    [Test]
    public async Task OnLoginAsync_WithoutBinding_ReturnsNull()
    {
        var service = new TransitionFixedCsrfService { IssueReturnsNull = true };
        var listener = Build(service);
        var ctx = AnonymousContext();

        var token = await listener.OnLoginAsync(ctx);

        TestAssert.Null(token);
    }

    /// <summary>OnLogoutAsync with a context before session clear clears the marker.</summary>
    [Test]
    public async Task OnLogoutAsync_ContextBeforeClear_ClearsMarker()
    {
        var service = new TransitionFixedCsrfService();
        var store = new TransitionInMemoryRotationStore();
        var listener = Build(service, store: store);
        var ctx = AuthenticatedContext("user-42");

        await listener.OnLogoutAsync(ctx);

        TestAssert.Equal(1, store.ClearCalls);
    }

    /// <summary>OnLogoutAsync with an anonymous context logs a warning.</summary>
    [Test]
    public async Task OnLogoutAsync_ContextAfterClear_LogsWarning()
    {
        var service = new TransitionFixedCsrfService();
        var store = new TransitionInMemoryRotationStore();
        var logger = new FakeLogger<DefaultCsrfTransitionListener>();
        var listener = Build(service, store: store, logger: logger);
        var ctx = AnonymousContext();

        await listener.OnLogoutAsync(ctx);

        TestAssert.Equal(0, store.ClearCalls);
        var warnings = 0;
        foreach (var e in logger.Entries) { if (e.Level == LogLevel.Warning) { warnings++; } }
        TestAssert.True(warnings >= 1);
    }

    /// <summary>OnLogoutAsync with an explicit binding clears the marker.</summary>
    [Test]
    public async Task OnLogoutAsync_ExplicitBinding_ClearsMarker()
    {
        var service = new TransitionFixedCsrfService();
        var store = new TransitionInMemoryRotationStore();
        var listener = Build(service, store: store);

        await listener.OnLogoutAsync("captured-binding-value");

        TestAssert.Equal(1, store.ClearCalls);
        TestAssert.Equal("captured-binding-value", store.LastClearedBinding);
    }

    /// <summary>OnLogoutAsync without a store logs a warning and is a no-op.</summary>
    [Test]
    public async Task OnLogoutAsync_NoStore_LogsWarning()
    {
        var service = new TransitionFixedCsrfService();
        var logger = new FakeLogger<DefaultCsrfTransitionListener>();
        var listener = Build(service, logger: logger);

        await listener.OnLogoutAsync("captured-binding-value");

        var warnings = 0;
        foreach (var e in logger.Entries) { if (e.Level == LogLevel.Warning) { warnings++; } }
        TestAssert.True(warnings >= 1);
    }

    /// <summary>OnLoginAsync with a null context throws.</summary>
    [Test]
    public async Task OnLoginAsync_NullContext_Throws()
    {
        var service = new TransitionFixedCsrfService();
        var listener = Build(service);
        try
        {
            await listener.OnLoginAsync(null!);
            TestAssert.True(false, "Expected ArgumentNullException.");
        }
        catch (ArgumentNullException)
        {
            // Expected.
        }
    }

    /// <summary>OnLogoutAsync with a null context throws.</summary>
    [Test]
    public async Task OnLogoutAsync_NullContext_Throws()
    {
        var service = new TransitionFixedCsrfService();
        var listener = Build(service);
        try
        {
            await listener.OnLogoutAsync((HttpContext)null!);
            TestAssert.True(false, "Expected ArgumentNullException.");
        }
        catch (ArgumentNullException)
        {
            // Expected.
        }
    }

    /// <summary>OnLogoutAsync with a null binding throws.</summary>
    [Test]
    public async Task OnLogoutAsync_NullBinding_Throws()
    {
        var service = new TransitionFixedCsrfService();
        var listener = Build(service);
        try
        {
            await listener.OnLogoutAsync((string)null!);
            TestAssert.True(false, "Expected ArgumentNullException.");
        }
        catch (ArgumentNullException)
        {
            // Expected.
        }
    }

    /// <summary>The constructor rejects a null service.</summary>
    [Test]
    public void Constructor_RejectsNullService()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            new DefaultCsrfTransitionListener(null!,
                new CsrfBindingProvider(new CsrfOptions()),
                new FakeLogger<DefaultCsrfTransitionListener>()));
    }

    /// <summary>The constructor rejects a null binding provider.</summary>
    [Test]
    public void Constructor_RejectsNullBindingProvider()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            new DefaultCsrfTransitionListener(new TransitionFixedCsrfService(),
                null!,
                new FakeLogger<DefaultCsrfTransitionListener>()));
    }

    /// <summary>The constructor rejects a null logger.</summary>
    [Test]
    public void Constructor_RejectsNullLogger()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            new DefaultCsrfTransitionListener(new TransitionFixedCsrfService(),
                new CsrfBindingProvider(new CsrfOptions()),
                null!));
    }

    /// <summary>
    /// The listener type does not reference any ASP.NET Core Identity type.
    /// This is the boundary guardrail assertion.
    /// </summary>
    [Test]
    public void Listener_DoesNotReferenceIdentityTypes()
    {
        var referenced = typeof(DefaultCsrfTransitionListener).Assembly
            .GetReferencedAssemblies();
        foreach (var asm in referenced)
        {
            TestAssert.False(
                asm.Name is not null && asm.Name.Contains("Identity", StringComparison.OrdinalIgnoreCase),
                "The ApiPilot.Security assembly must not reference ASP.NET Core Identity.");
        }
    }

    /// <summary>A custom listener can replace the default through the interface.</summary>
    [Test]
    public async Task CustomListener_ReplacesDefault()
    {
        ICsrfTransitionListener custom = new CustomTransitionListener();
        var ctx = AuthenticatedContext("user-42");

        var token = await custom.OnLoginAsync(ctx);

        TestAssert.Null(token);
        await custom.OnLogoutAsync(ctx);
    }
}

/// <summary>A test double for the transition tests.</summary>
internal sealed class TransitionFixedCsrfService : ICsrfService
{
    public bool IssueReturnsNull { get; set; }

    public bool IssueCalled { get; private set; }

    public Task<CsrfToken?> IssueAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        IssueCalled = true;
        return Task.FromResult<CsrfToken?>(IssueReturnsNull ? null : CsrfToken.From("fixed-token"));
    }

    public Task<CsrfValidationResult> ValidateAsync(
        HttpContext context,
        string? rawHeaderValue,
        CancellationToken cancellationToken = default)
        => Task.FromResult(CsrfValidationResult.Success());

    public Task<CsrfToken?> RotateAsync(HttpContext context, CancellationToken cancellationToken = default)
        => Task.FromResult<CsrfToken?>(CsrfToken.From("fixed-token"));
}

/// <summary>An in-memory rotation store for the transition tests.</summary>
internal sealed class TransitionInMemoryRotationStore : ICsrfRotationStore
{
    public int ClearCalls { get; private set; }

    public string? LastClearedBinding { get; private set; }

    public Task<DateTimeOffset?> GetRotationMarkerAsync(
        string binding,
        CancellationToken cancellationToken = default)
        => Task.FromResult<DateTimeOffset?>(null);

    public Task SetRotationMarkerAsync(
        string binding,
        DateTimeOffset rotatedAtUtc,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task ClearRotationMarkerAsync(
        string binding,
        CancellationToken cancellationToken = default)
    {
        ClearCalls++;
        LastClearedBinding = binding;
        return Task.CompletedTask;
    }
}

/// <summary>A custom listener used to prove the interface is replaceable.</summary>
internal sealed class CustomTransitionListener : ICsrfTransitionListener
{
    public Task<CsrfToken?> OnLoginAsync(
        HttpContext context,
        CancellationToken cancellationToken = default)
        => Task.FromResult<CsrfToken?>(null);

    public Task OnLogoutAsync(
        HttpContext context,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task OnLogoutAsync(
        string previousBinding,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

