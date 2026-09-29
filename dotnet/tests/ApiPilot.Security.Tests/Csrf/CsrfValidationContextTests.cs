// filepath: dotnet/tests/ApiPilot.Security.Tests/Csrf/CsrfValidationContextTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Contract tests for the CsrfValidationContext value object
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Security.Csrf
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : CsrfValidationContext.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.Csrf;

namespace ApiPilot.Security.Tests.Csrf;

/// <summary>
/// Contract tests for CsrfValidationContext. Verifies the two factories,
/// their rejection of invalid inputs, and the shape of the produced value.
/// </summary>
[TestClass]
public sealed class CsrfValidationContextTests
{
    /// <summary>WithBinding carries the binding and a None failure reason.</summary>
    [Test]
    public void WithBinding_ValidBinding_CarriesBinding()
    {
        var ctx = CsrfValidationContext.WithBinding("binding-value", "header-value");
        TestAssert.Equal("binding-value", ctx.Binding);
        TestAssert.Equal(CsrfBindingFailureReason.None, ctx.BindingFailure);
        TestAssert.Equal("header-value", ctx.RawHeaderValue);
    }

    /// <summary>WithBinding accepts a null raw header value.</summary>
    [Test]
    public void WithBinding_NullRawHeader_Accepts()
    {
        var ctx = CsrfValidationContext.WithBinding("binding-value", null);
        TestAssert.Equal("binding-value", ctx.Binding);
        TestAssert.Null(ctx.RawHeaderValue);
    }

    /// <summary>WithBinding rejects a null binding.</summary>
    [Test]
    public void WithBinding_NullBinding_Throws()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            CsrfValidationContext.WithBinding(null!, "header-value"));
    }

    /// <summary>WithBinding rejects an empty binding.</summary>
    [Test]
    public void WithBinding_EmptyBinding_Throws()
    {
        TestAssert.Throws<ArgumentException>(() =>
            CsrfValidationContext.WithBinding("", "header-value"));
    }

    /// <summary>WithoutBinding carries the failure reason and no binding.</summary>
    [Test]
    public void WithoutBinding_ValidFailure_CarriesFailure()
    {
        var ctx = CsrfValidationContext.WithoutBinding(
            CsrfBindingFailureReason.NoSubject,
            "header-value");
        TestAssert.Null(ctx.Binding);
        TestAssert.Equal(CsrfBindingFailureReason.NoSubject, ctx.BindingFailure);
        TestAssert.Equal("header-value", ctx.RawHeaderValue);
    }

    /// <summary>WithoutBinding rejects a None failure reason.</summary>
    [Test]
    public void WithoutBinding_NoneReason_Throws()
    {
        TestAssert.Throws<ArgumentException>(() =>
            CsrfValidationContext.WithoutBinding(
                CsrfBindingFailureReason.None,
                "header-value"));
    }
}

