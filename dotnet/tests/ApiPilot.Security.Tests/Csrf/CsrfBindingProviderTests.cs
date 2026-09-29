// filepath: dotnet/tests/ApiPilot.Security.Tests/Csrf/CsrfBindingProviderTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Contract tests for the default CSRF binding provider
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class + TestSession double)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.Security.Csrf, Microsoft.AspNetCore.Http
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : CsrfBindingProvider.cs, ICsrfBindingProvider.cs
// -----------------------------------------------------------------------------

using System.Security.Claims;
using ApiPilot.Security.Csrf;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace ApiPilot.Security.Tests.Csrf;

/// <summary>
/// Contract tests for CsrfBindingProvider. Verifies the three resolution
/// sources (subject claim, session, pre-auth), the fail-closed behavior,
/// the fingerprint stability, and the cross-source distinctness.
/// </summary>
[TestClass]
public sealed class CsrfBindingProviderTests
{
    private static CsrfBindingProvider Build(Action<CsrfOptions>? configure = null)
    {
        var options = new CsrfOptions();
        configure?.Invoke(options);
        return new CsrfBindingProvider(options);
    }

    private static DefaultHttpContext Context()
    {
        return new DefaultHttpContext();
    }

    private static ClaimsPrincipal AuthenticatedUser(string subjectClaimType, string subjectValue)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(subjectClaimType, subjectValue),
        }, authenticationType: "Test");
        return new ClaimsPrincipal(identity);
    }

    /// <summary>An authenticated user with a sub claim produces a binding.</summary>
    [Test]
    public void TryGetBinding_SubClaim_ProducesBinding()
    {
        var provider = Build();
        var ctx = Context();
        ctx.User = AuthenticatedUser("sub", "user-42");

        var ok = provider.TryGetBinding(ctx, out var binding, out var reason);

        TestAssert.True(ok);
        TestAssert.True(binding.Length > 0);
        TestAssert.Equal(CsrfBindingFailureReason.None, reason);
    }

    /// <summary>The same sub claim produces the same binding across requests.</summary>
    [Test]
    public void TryGetBinding_SameSubject_ProducesStableBinding()
    {
        var provider = Build();
        var ctxA = Context();
        ctxA.User = AuthenticatedUser("sub", "user-42");
        var ctxB = Context();
        ctxB.User = AuthenticatedUser("sub", "user-42");

        provider.TryGetBinding(ctxA, out var bindingA, out _);
        provider.TryGetBinding(ctxB, out var bindingB, out _);

        TestAssert.Equal(bindingA, bindingB);
    }

    /// <summary>Different sub claims produce different bindings.</summary>
    [Test]
    public void TryGetBinding_DifferentSubjects_ProducesDifferentBindings()
    {
        var provider = Build();
        var ctxA = Context();
        ctxA.User = AuthenticatedUser("sub", "user-42");
        var ctxB = Context();
        ctxB.User = AuthenticatedUser("sub", "user-99");

        provider.TryGetBinding(ctxA, out var bindingA, out _);
        provider.TryGetBinding(ctxB, out var bindingB, out _);

        TestAssert.NotEqual(bindingA, bindingB);
    }

    /// <summary>The binding is not the raw subject value.</summary>
    [Test]
    public void TryGetBinding_BindingIsNotTheRawSubject()
    {
        var provider = Build();
        var ctx = Context();
        ctx.User = AuthenticatedUser("sub", "user-42");

        provider.TryGetBinding(ctx, out var binding, out _);

        TestAssert.False(binding.Contains("user-42", StringComparison.Ordinal));
    }

    /// <summary>An authenticated user with only NameIdentifier produces a binding.</summary>
    [Test]
    public void TryGetBinding_NameIdentifierClaim_ProducesBinding()
    {
        var provider = Build();
        var ctx = Context();
        ctx.User = AuthenticatedUser(ClaimTypes.NameIdentifier, "user-42");

        var ok = provider.TryGetBinding(ctx, out var binding, out _);

        TestAssert.True(ok);
        TestAssert.True(binding.Length > 0);
    }

    /// <summary>An unauthenticated user with no session fails with NoSession.</summary>
    [Test]
    public void TryGetBinding_NoUserNoSession_FailsWithNoSession()
    {
        var provider = Build();
        var ctx = Context();

        var ok = provider.TryGetBinding(ctx, out var binding, out var reason);

        TestAssert.False(ok);
        TestAssert.Equal("", binding);
        TestAssert.True(reason == CsrfBindingFailureReason.NoSession
            || reason == CsrfBindingFailureReason.NoConfiguredSource);
    }

    /// <summary>An available session produces a binding.</summary>
    [Test]
    public void TryGetBinding_AvailableSession_ProducesBinding()
    {
        var provider = Build();
        var ctx = Context();
        ctx.Features.Set<ISessionFeature>(new TestSessionFeature(new TestSession("session-abc123")));

        var ok = provider.TryGetBinding(ctx, out var binding, out _);

        TestAssert.True(ok);
        TestAssert.True(binding.Length > 0);
    }

    /// <summary>The pre-auth source is used when no subject and no session are present.</summary>
    [Test]
    public void TryGetBinding_PreAuthSource_ProducesBinding()
    {
        var provider = Build(o => o.PreAuthBindingSource = _ => "preauth-42");
        var ctx = Context();

        var ok = provider.TryGetBinding(ctx, out var binding, out _);

        TestAssert.True(ok);
        TestAssert.True(binding.Length > 0);
    }

    /// <summary>A pre-auth source that returns null fails with ProviderFailure.</summary>
    [Test]
    public void TryGetBinding_PreAuthSourceReturnsNull_FailsWithProviderFailure()
    {
        var provider = Build(o => o.PreAuthBindingSource = _ => null);
        var ctx = Context();

        var ok = provider.TryGetBinding(ctx, out _, out var reason);

        TestAssert.False(ok);
        TestAssert.Equal(CsrfBindingFailureReason.ProviderFailure, reason);
    }

    /// <summary>A pre-auth source that returns empty fails with ProviderFailure.</summary>
    [Test]
    public void TryGetBinding_PreAuthSourceReturnsEmpty_FailsWithProviderFailure()
    {
        var provider = Build(o => o.PreAuthBindingSource = _ => "");
        var ctx = Context();

        var ok = provider.TryGetBinding(ctx, out _, out var reason);

        TestAssert.False(ok);
        TestAssert.Equal(CsrfBindingFailureReason.ProviderFailure, reason);
    }

    /// <summary>A subject binding and a session binding with the same raw value differ.</summary>
    [Test]
    public void TryGetBinding_SubjectAndSessionWithSameValue_ProduceDifferentBindings()
    {
        var provider = Build();

        var ctxSubject = Context();
        ctxSubject.User = AuthenticatedUser("sub", "abc123");
        provider.TryGetBinding(ctxSubject, out var subjectBinding, out _);

        var ctxSession = Context();
        ctxSession.Features.Set<ISessionFeature>(new TestSessionFeature(new TestSession("abc123")));
        provider.TryGetBinding(ctxSession, out var sessionBinding, out _);

        // The subject and session fingerprint input prefixes differ, so
        // the same raw value produces different bindings.
        TestAssert.NotEqual(subjectBinding, sessionBinding);
    }

    /// <summary>A null context is rejected.</summary>
    [Test]
    public void TryGetBinding_NullContext_Throws()
    {
        var provider = Build();
        TestAssert.Throws<ArgumentNullException>(() =>
            provider.TryGetBinding(null!, out _, out _));
    }

    /// <summary>A null options argument is rejected.</summary>
    [Test]
    public void Constructor_RejectsNullOptions()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            new CsrfBindingProvider(null!));
    }
}

/// <summary>A minimal ISession for tests, carrying only Id and IsAvailable.</summary>
internal sealed class TestSession : ISession
{
    private readonly string _id;

    public TestSession(string id)
    {
        _id = id;
    }

    public string Id => _id;

    public bool IsAvailable => true;

    public IEnumerable<string> Keys => Array.Empty<string>();

    public void Clear() { }

    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Remove(string key) { }

    public void Set(string key, byte[] value) { }

    public bool TryGetValue(string key, out byte[] value)
    {
        value = Array.Empty<byte>();
        return false;
    }
}

/// <summary>An ISessionFeature implementation wrapping a TestSession.</summary>
internal sealed class TestSessionFeature : ISessionFeature
{
    public TestSessionFeature(ISession session)
    {
        Session = session;
    }

    public ISession Session { get; set; }
}

