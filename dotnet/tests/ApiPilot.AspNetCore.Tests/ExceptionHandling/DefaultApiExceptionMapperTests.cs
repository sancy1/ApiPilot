// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/ExceptionHandling/DefaultApiExceptionMapperTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Contract tests for the DefaultApiExceptionMapper
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.AspNetCore.ExceptionHandling, ApiPilot.Core.Errors,
//                Microsoft.AspNetCore.Http
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : DefaultApiExceptionMapper.cs, KnownExceptionTypes.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.AspNetCore.ExceptionHandling;
using ApiPilot.Core.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ApiPilot.AspNetCore.Tests.ExceptionHandling;

/// <summary>
/// Contract tests for DefaultApiExceptionMapper. Verifies the built-in
/// mappings, the ordering rule that specific types match before base types,
/// and the fallback behavior for unknown exceptions.
/// </summary>
[TestClass]
public sealed class DefaultApiExceptionMapperTests
{
    private static DefaultApiExceptionMapper Mapper(Action<ApiExceptionOptions>? configure = null)
    {
        var concrete = new ApiExceptionOptions();
        configure?.Invoke(concrete);
        return new DefaultApiExceptionMapper(Options.Create(concrete));
    }

    private static HttpContext Context() => new DefaultHttpContext();

    /// <summary>ArgumentNullException maps to VALIDATION_ERROR.</summary>
    [Test]
    public void Map_ArgumentNullException_MapsToValidationError()
    {
        var error = Mapper().Map(new ArgumentNullException("x"), Context());
        TestAssert.Equal("VALIDATION_ERROR", error.Code.Code);
    }

    /// <summary>ArgumentException maps to VALIDATION_ERROR.</summary>
    [Test]
    public void Map_ArgumentException_MapsToValidationError()
    {
        var error = Mapper().Map(new ArgumentException("bad"), Context());
        TestAssert.Equal("VALIDATION_ERROR", error.Code.Code);
    }

    /// <summary>KeyNotFoundException maps to RESOURCE_NOT_FOUND.</summary>
    [Test]
    public void Map_KeyNotFoundException_MapsToResourceNotFound()
    {
        var error = Mapper().Map(new KeyNotFoundException(), Context());
        TestAssert.Equal("RESOURCE_NOT_FOUND", error.Code.Code);
    }

    /// <summary>UnauthorizedAccessException maps to FORBIDDEN.</summary>
    [Test]
    public void Map_UnauthorizedAccessException_MapsToForbidden()
    {
        var error = Mapper().Map(new UnauthorizedAccessException(), Context());
        TestAssert.Equal("FORBIDDEN", error.Code.Code);
    }

    /// <summary>InvalidOperationException maps to CONFLICT.</summary>
    [Test]
    public void Map_InvalidOperationException_MapsToConflict()
    {
        var error = Mapper().Map(new InvalidOperationException(), Context());
        TestAssert.Equal("CONFLICT", error.Code.Code);
    }

    /// <summary>ArgumentNullException matches its specific mapping, not the base.</summary>
    [Test]
    public void Map_ArgumentNullException_MatchesSpecificBeforeBase()
    {
        var error = Mapper().Map(new ArgumentNullException("x"), Context());
        // Both map to VALIDATION_ERROR, but the message comes from the
        // ArgumentNullException entry because it is listed first.
        TestAssert.Contains("missing", error.Message);
    }

    /// <summary>An unmapped exception falls back to INTERNAL_ERROR.</summary>
    [Test]
    public void Map_UnknownException_FallsBackToInternalError()
    {
        var error = Mapper().Map(new InvalidCastException(), Context());
        TestAssert.Equal("INTERNAL_ERROR", error.Code.Code);
    }

    /// <summary>The fallback message is generic by default.</summary>
    [Test]
    public void Map_UnknownException_UsesGenericMessage()
    {
        var error = Mapper().Map(new InvalidCastException("secret-detail"), Context());
        TestAssert.False(error.Message.Contains("secret-detail"));
    }

    /// <summary>With RevealExceptionMessageInResponse, the fallback message is the exception message.</summary>
    [Test]
    public void Map_UnknownException_WithRevealMessage_UsesExceptionMessage()
    {
        var error = Mapper(o => o.RevealExceptionMessageInResponse = true)
            .Map(new InvalidCastException("visible-detail"), Context());
        TestAssert.Contains("visible-detail", error.Message);
    }

    /// <summary>With RevealExceptionTypeInResponse, the type name appears in the fallback message.</summary>
    [Test]
    public void Map_UnknownException_WithRevealType_AppendsTypeName()
    {
        var error = Mapper(o => o.RevealExceptionTypeInResponse = true)
            .Map(new InvalidCastException(), Context());
        TestAssert.Contains("InvalidCastException", error.Message);
    }

    /// <summary>A custom mapping with IncludeExceptionMessage uses the exception message.</summary>
    [Test]
    public void Map_KnownMapping_WithIncludeExceptionMessage_UsesExceptionMessage()
    {
        var error = Mapper(o =>
        {
            o.Mappings.Insert(0, new KnownExceptionType(
                typeof(InvalidCastException),
                ApiErrorCode.Conflict,
                "safe default",
                IncludeExceptionMessage: true));
        }).Map(new InvalidCastException("specific-detail"), Context());
        TestAssert.Equal("CONFLICT", error.Code.Code);
        TestAssert.Equal("specific-detail", error.Message);
    }

    /// <summary>Null exception is rejected.</summary>
    [Test]
    public void Map_RejectsNullException()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            Mapper().Map(null!, Context()));
    }

    /// <summary>Null HttpContext is rejected.</summary>
    [Test]
    public void Map_RejectsNullHttpContext()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            Mapper().Map(new InvalidOperationException("x"), null!));
    }
}

