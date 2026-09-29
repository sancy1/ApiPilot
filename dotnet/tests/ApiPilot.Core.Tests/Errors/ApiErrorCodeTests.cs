// filepath: dotnet/tests/ApiPilot.Core.Tests/Errors/ApiErrorCodeTests.cs
// layer: Tests | package: ApiPilot.Core.Tests | since: v0.1.0-alpha.0
// purpose: Contract tests for the ApiErrorCode value object
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Core.Errors
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiErrorCode.cs, SPEC.md (error code table)
// -----------------------------------------------------------------------------

using ApiPilot.Core.Errors;

namespace ApiPilot.Core.Tests.Errors;

/// <summary>
/// Contract tests for the ApiErrorCode value object. Verifies normalization
/// behavior, rejection of invalid inputs, structural equality, and the wire
/// value of each standard code.
/// </summary>
[TestClass]
public sealed class ApiErrorCodeTests
{
    /// <summary>From normalizes the input to uppercase.</summary>
    [Test]
    public void From_LowercaseInput_NormalizesToUppercase()
    {
        var code = ApiErrorCode.From("validation_error");
        TestAssert.Equal("VALIDATION_ERROR", code.Code);
    }

    /// <summary>From trims surrounding whitespace before normalizing.</summary>
    [Test]
    public void From_WithWhitespace_TrimsAndNormalizes()
    {
        var code = ApiErrorCode.From("  validation_error  ");
        TestAssert.Equal("VALIDATION_ERROR", code.Code);
    }

    /// <summary>From rejects null input.</summary>
    [Test]
    public void From_WithNull_Throws()
    {
        TestAssert.Throws<ArgumentNullException>(() => ApiErrorCode.From(null!));
    }

    /// <summary>From rejects empty input.</summary>
    [Test]
    public void From_WithEmpty_Throws()
    {
        TestAssert.Throws<ArgumentException>(() => ApiErrorCode.From(""));
    }

    /// <summary>From rejects whitespace-only input.</summary>
    [Test]
    public void From_WithWhitespaceOnly_Throws()
    {
        TestAssert.Throws<ArgumentException>(() => ApiErrorCode.From("   "));
    }

    /// <summary>ToString returns the wire value.</summary>
    [Test]
    public void ToString_ReturnsCodeValue()
    {
        var code = ApiErrorCode.From("conflict");
        TestAssert.Equal("CONFLICT", code.ToString());
    }

    /// <summary>Two codes with the same normalized value are equal.</summary>
    [Test]
    public void Equality_SameValue_AreEqual()
    {
        var a = ApiErrorCode.From("validation_error");
        var b = ApiErrorCode.From("VALIDATION_ERROR");
        TestAssert.True(a == b);
        TestAssert.True(a.Equals(b));
    }

    /// <summary>Two codes with different values are not equal.</summary>
    [Test]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = ApiErrorCode.From("validation_error");
        var b = ApiErrorCode.From("conflict");
        TestAssert.False(a == b);
        TestAssert.False(a.Equals(b));
    }

    /// <summary>Every standard code carries the correct wire value.</summary>
    [Test]
    public void StandardCodes_CarryCorrectWireValues()
    {
        TestAssert.Equal("VALIDATION_ERROR", ApiErrorCode.ValidationError.Code);
        TestAssert.Equal("AUTHENTICATION_REQUIRED", ApiErrorCode.AuthenticationRequired.Code);
        TestAssert.Equal("FORBIDDEN", ApiErrorCode.Forbidden.Code);
        TestAssert.Equal("CSRF_HEADER_MISSING", ApiErrorCode.CsrfHeaderMissing.Code);
        TestAssert.Equal("CSRF_TOKEN_INVALID", ApiErrorCode.CsrfTokenInvalid.Code);
        TestAssert.Equal("CSRF_TOKEN_EXPIRED", ApiErrorCode.CsrfTokenExpired.Code);
        TestAssert.Equal("CSRF_ORIGIN_REJECTED", ApiErrorCode.CsrfOriginRejected.Code);
        TestAssert.Equal("RESOURCE_NOT_FOUND", ApiErrorCode.ResourceNotFound.Code);
        TestAssert.Equal("CONFLICT", ApiErrorCode.Conflict.Code);
        TestAssert.Equal("RATE_LIMITED", ApiErrorCode.RateLimited.Code);
        TestAssert.Equal("INTERNAL_ERROR", ApiErrorCode.InternalError.Code);
        TestAssert.Equal("CONFIGURATION_ERROR", ApiErrorCode.ConfigurationError.Code);
        TestAssert.Equal("NOT_ACCEPTABLE", ApiErrorCode.NotAcceptable.Code);
        TestAssert.Equal("UNSUPPORTED_MEDIA_TYPE", ApiErrorCode.UnsupportedMediaType.Code);
    }

    /// <summary>The standard static properties return stable instances.</summary>
    [Test]
    public void StandardCodes_ReturnStableInstances()
    {
        var a = ApiErrorCode.ValidationError;
        var b = ApiErrorCode.ValidationError;
        TestAssert.True(ReferenceEquals(a, b));
    }
}

