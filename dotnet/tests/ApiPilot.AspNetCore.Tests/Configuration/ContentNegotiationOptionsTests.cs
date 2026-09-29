// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Configuration/ContentNegotiationOptionsTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Contract tests for ContentNegotiationOptions
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Core.Configuration
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ContentNegotiationOptions.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Configuration;

namespace ApiPilot.AspNetCore.Tests.Configuration;

/// <summary>
/// Contract tests for ContentNegotiationOptions. Verifies defaults and
/// mutability of the settable properties.
/// </summary>
[TestClass]
public sealed class ContentNegotiationOptionsTests
{
    /// <summary>AcceptableResponseMediaTypes defaults to application/json.</summary>
    [Test]
    public void Defaults_AcceptableResponseMediaTypes_ContainsApplicationJson()
    {
        var options = new ContentNegotiationOptions();
        TestAssert.Equal(1, options.AcceptableResponseMediaTypes.Count);
        TestAssert.True(options.AcceptableResponseMediaTypes.Contains("application/json"));
    }

    /// <summary>AcceptableRequestMediaTypes defaults to application/json.</summary>
    [Test]
    public void Defaults_AcceptableRequestMediaTypes_ContainsApplicationJson()
    {
        var options = new ContentNegotiationOptions();
        TestAssert.Equal(1, options.AcceptableRequestMediaTypes.Count);
        TestAssert.True(options.AcceptableRequestMediaTypes.Contains("application/json"));
    }

    /// <summary>AcceptWildcard defaults to true.</summary>
    [Test]
    public void Defaults_AcceptWildcard_IsTrue()
    {
        var options = new ContentNegotiationOptions();
        TestAssert.True(options.AcceptWildcard);
    }

    /// <summary>AcceptMissingContentType defaults to false.</summary>
    [Test]
    public void Defaults_AcceptMissingContentType_IsFalse()
    {
        var options = new ContentNegotiationOptions();
        TestAssert.False(options.AcceptMissingContentType);
    }

    /// <summary>BodyCarryingMethods defaults to POST, PUT, PATCH.</summary>
    [Test]
    public void Defaults_BodyCarryingMethods_ContainsPostPutPatch()
    {
        var options = new ContentNegotiationOptions();
        TestAssert.Equal(3, options.BodyCarryingMethods.Count);
        TestAssert.True(options.BodyCarryingMethods.Contains("POST"));
        TestAssert.True(options.BodyCarryingMethods.Contains("PUT"));
        TestAssert.True(options.BodyCarryingMethods.Contains("PATCH"));
    }

    /// <summary>The settable properties accept new values.</summary>
    [Test]
    public void Properties_AreMutable()
    {
        var options = new ContentNegotiationOptions();
        options.AcceptWildcard = false;
        options.AcceptMissingContentType = true;
        options.AcceptableResponseMediaTypes.Add("application/problem+json");
        options.AcceptableRequestMediaTypes.Add("application/vnd.api+json");
        options.BodyCarryingMethods.Add("DELETE");
        TestAssert.False(options.AcceptWildcard);
        TestAssert.True(options.AcceptMissingContentType);
        TestAssert.Equal(2, options.AcceptableResponseMediaTypes.Count);
        TestAssert.Equal(2, options.AcceptableRequestMediaTypes.Count);
        TestAssert.Equal(4, options.BodyCarryingMethods.Count);
    }
}

