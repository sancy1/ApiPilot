// filepath: dotnet/tests/ApiPilot.Security.Tests/Csrf/CsrfOptionsTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Contract tests for the CsrfOptions S6 through S9 defaults and overrides
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Security.Csrf
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : CsrfOptions.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.Csrf;

namespace ApiPilot.Security.Tests.Csrf;

/// <summary>
/// Contract tests for CsrfOptions. Verifies the S6 through S9 defaults,
/// the code-mapping dictionary contents, and the two enum-shaped
/// properties.
/// </summary>
[TestClass]
public sealed class CsrfOptionsTests
{
    /// <summary>S6: HeaderName defaults to X-CSRF-TOKEN.</summary>
    [Test]
    public void Defaults_HeaderName_IsXCsrfToken()
    {
        var options = new CsrfOptions();
        TestAssert.Equal("X-CSRF-TOKEN", options.HeaderName);
    }

    /// <summary>S6: HeaderName is mutable.</summary>
    [Test]
    public void HeaderName_IsMutable()
    {
        var options = new CsrfOptions { HeaderName = "X-CSRF" };
        TestAssert.Equal("X-CSRF", options.HeaderName);
    }

    /// <summary>S7: RotationPolicy defaults to OnBootstrap.</summary>
    [Test]
    public void Defaults_RotationPolicy_IsOnBootstrap()
    {
        var options = new CsrfOptions();
        TestAssert.Equal(CsrfRotationPolicy.OnBootstrap, options.RotationPolicy);
    }

    /// <summary>CsrfRotationPolicy has three named members.</summary>
    [Test]
    public void CsrfRotationPolicy_HasThreeMembers()
    {
        var values = Enum.GetValues<CsrfRotationPolicy>();
        TestAssert.Equal(3, values.Length);
        TestAssert.True(Array.IndexOf(values, CsrfRotationPolicy.OnBootstrap) >= 0);
        TestAssert.True(Array.IndexOf(values, CsrfRotationPolicy.OnWindow) >= 0);
        TestAssert.True(Array.IndexOf(values, CsrfRotationPolicy.Never) >= 0);
    }

    /// <summary>CsrfRotationRequirement has two named members.</summary>
    [Test]
    public void CsrfRotationRequirement_HasTwoMembers()
    {
        var values = Enum.GetValues<CsrfRotationRequirement>();
        TestAssert.Equal(2, values.Length);
        TestAssert.True(Array.IndexOf(values, CsrfRotationRequirement.Optional) >= 0);
        TestAssert.True(Array.IndexOf(values, CsrfRotationRequirement.Required) >= 0);
    }

    /// <summary>S8: BootstrapPath defaults to /api/csrf.</summary>
    [Test]
    public void Defaults_BootstrapPath_IsApiCsrf()
    {
        var options = new CsrfOptions();
        TestAssert.Equal("/api/csrf", options.BootstrapPath);
    }

    /// <summary>S8: BootstrapPath is mutable.</summary>
    [Test]
    public void BootstrapPath_IsMutable()
    {
        var options = new CsrfOptions { BootstrapPath = "/csrf" };
        TestAssert.Equal("/csrf", options.BootstrapPath);
    }

    /// <summary>MissingHeaderCode defaults to CSRF_HEADER_MISSING.</summary>
    [Test]
    public void Defaults_MissingHeaderCode_IsCsrfHeaderMissing()
    {
        var options = new CsrfOptions();
        TestAssert.Equal("CSRF_HEADER_MISSING", options.MissingHeaderCode);
    }

    /// <summary>S9: CodeMapping carries the six default mappings.</summary>
    [Test]
    public void Defaults_CodeMapping_HasSevenEntries()
    {
        var options = new CsrfOptions();
        TestAssert.Equal(7, options.CodeMapping.Count);
        TestAssert.Equal("CSRF_TOKEN_INVALID", options.CodeMapping[CsrfTokenParseResult.Malformed]);
        TestAssert.Equal("CSRF_TOKEN_INVALID", options.CodeMapping[CsrfTokenParseResult.WrongVersion]);
        TestAssert.Equal("CSRF_TOKEN_INVALID", options.CodeMapping[CsrfTokenParseResult.InvalidSignature]);
        TestAssert.Equal("CSRF_TOKEN_INVALID", options.CodeMapping[CsrfTokenParseResult.WrongSession]);
        TestAssert.Equal("CSRF_TOKEN_INVALID", options.CodeMapping[CsrfTokenParseResult.Rotated]);
        TestAssert.Equal("CSRF_TOKEN_INVALID", options.CodeMapping[CsrfTokenParseResult.BindingMissing]);
        TestAssert.Equal("CSRF_TOKEN_EXPIRED", options.CodeMapping[CsrfTokenParseResult.Expired]);
    }

    /// <summary>S9: CodeMapping omits Ok by design.</summary>
    [Test]
    public void CodeMapping_DoesNotContainOk()
    {
        var options = new CsrfOptions();
        TestAssert.False(options.CodeMapping.ContainsKey(CsrfTokenParseResult.Ok));
    }

    /// <summary>S9: CodeMapping is mutable for overrides.</summary>
    [Test]
    public void CodeMapping_IsMutable()
    {
        var options = new CsrfOptions();
        options.CodeMapping[CsrfTokenParseResult.Expired] = "CUSTOM_EXPIRED";
        TestAssert.Equal("CUSTOM_EXPIRED", options.CodeMapping[CsrfTokenParseResult.Expired]);
    }

    /// <summary>PreAuthBindingSource defaults to null.</summary>
    [Test]
    public void Defaults_PreAuthBindingSource_IsNull()
    {
        var options = new CsrfOptions();
        TestAssert.Null(options.PreAuthBindingSource);
    }

    /// <summary>PreAuthBindingSource accepts a delegate.</summary>
    [Test]
    public void PreAuthBindingSource_AcceptsDelegate()
    {
        var options = new CsrfOptions
        {
            PreAuthBindingSource = _ => "preauth-binding",
        };
        TestAssert.NotNull(options.PreAuthBindingSource);
        TestAssert.Equal("preauth-binding", options.PreAuthBindingSource!(null!));
    }

    /// <summary>RotationRequirement defaults to Optional.</summary>
    [Test]
    public void Defaults_RotationRequirement_IsOptional()
    {
        var options = new CsrfOptions();
        TestAssert.Equal(CsrfRotationRequirement.Optional, options.RotationRequirement);
    }
}

