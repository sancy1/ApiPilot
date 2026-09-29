// filepath: dotnet/tests/ApiPilot.Security.Tests/Csrf/CsrfValidationResultTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Contract tests for the CsrfValidationResult value object
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Security.Csrf
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : CsrfValidationResult.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.Csrf;

namespace ApiPilot.Security.Tests.Csrf;

/// <summary>
/// Contract tests for CsrfValidationResult. Verifies the two factories,
/// their input validation, and the IsSuccess property.
/// </summary>
[TestClass]
public sealed class CsrfValidationResultTests
{
    /// <summary>Success produces Reason Ok with an empty public code.</summary>
    [Test]
    public void Success_ProducesOkWithEmptyCode()
    {
        var result = CsrfValidationResult.Success();
        TestAssert.Equal(CsrfTokenParseResult.Ok, result.Reason);
        TestAssert.Equal("", result.PublicCode);
        TestAssert.True(result.IsSuccess);
    }

    /// <summary>Failure carries the reason and the public code.</summary>
    [Test]
    public void Failure_CarriesReasonAndCode()
    {
        var result = CsrfValidationResult.Failure(
            CsrfTokenParseResult.InvalidSignature,
            "CSRF_TOKEN_INVALID");
        TestAssert.Equal(CsrfTokenParseResult.InvalidSignature, result.Reason);
        TestAssert.Equal("CSRF_TOKEN_INVALID", result.PublicCode);
        TestAssert.False(result.IsSuccess);
    }

    /// <summary>Failure rejects the Ok reason.</summary>
    [Test]
    public void Failure_WithOkReason_Throws()
    {
        TestAssert.Throws<ArgumentException>(() =>
            CsrfValidationResult.Failure(CsrfTokenParseResult.Ok, "CSRF_TOKEN_INVALID"));
    }

    /// <summary>Failure rejects an empty public code.</summary>
    [Test]
    public void Failure_WithEmptyCode_Throws()
    {
        TestAssert.Throws<ArgumentException>(() =>
            CsrfValidationResult.Failure(CsrfTokenParseResult.Expired, ""));
    }

    /// <summary>Failure rejects a null public code.</summary>
    [Test]
    public void Failure_WithNullCode_Throws()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            CsrfValidationResult.Failure(CsrfTokenParseResult.Expired, null!));
    }

    /// <summary>IsSuccess is true only when Reason is Ok.</summary>
    [Test]
    public void IsSuccess_IsTrueOnlyWhenReasonIsOk()
    {
        TestAssert.True(CsrfValidationResult.Success().IsSuccess);
        TestAssert.False(CsrfValidationResult.Failure(
            CsrfTokenParseResult.Expired, "CSRF_TOKEN_EXPIRED").IsSuccess);
        TestAssert.False(CsrfValidationResult.Failure(
            CsrfTokenParseResult.Rotated, "CSRF_TOKEN_INVALID").IsSuccess);
        TestAssert.False(CsrfValidationResult.Failure(
            CsrfTokenParseResult.BindingMissing, "CSRF_TOKEN_INVALID").IsSuccess);
    }
}

