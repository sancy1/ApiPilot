// filepath: dotnet/tests/ApiPilot.Security.Tests/Csrf/CsrfAttributesTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Contract tests for the CSRF protection attributes
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Security.Csrf,
//                Microsoft.AspNetCore.Http.Metadata
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : CsrfAttributes.cs
// -----------------------------------------------------------------------------

using System.Reflection;
using ApiPilot.Security.Csrf;
using Microsoft.AspNetCore.Http.Metadata;

namespace ApiPilot.Security.Tests.Csrf;

/// <summary>
/// Contract tests for the [ApiPilotSkipCsrf] and [ApiPilotRequireCsrf]
/// attributes. Verifies the AttributeUsage metadata and the interface
/// implementation.
/// </summary>
[TestClass]
public sealed class CsrfAttributesTests
{
    /// <summary>Skip is applicable to classes and methods, not inherited-multiple.</summary>
    [Test]
    public void SkipCsrf_HasCorrectAttributeUsage()
    {
        var usage = typeof(ApiPilotSkipCsrfAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();
        TestAssert.NotNull(usage);
        TestAssert.True((usage!.ValidOn & AttributeTargets.Class) != 0);
        TestAssert.True((usage.ValidOn & AttributeTargets.Method) != 0);
        TestAssert.False(usage.AllowMultiple);
    }

    /// <summary>Require is applicable to classes and methods, not inherited-multiple.</summary>
    [Test]
    public void RequireCsrf_HasCorrectAttributeUsage()
    {
        var usage = typeof(ApiPilotRequireCsrfAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();
        TestAssert.NotNull(usage);
        TestAssert.True((usage!.ValidOn & AttributeTargets.Class) != 0);
        TestAssert.True((usage.ValidOn & AttributeTargets.Method) != 0);
        TestAssert.False(usage.AllowMultiple);
    }

    /// <summary>Both attributes implement IEndpointMetadataProvider.</summary>
    [Test]
    public void BothAttributes_ImplementIEndpointMetadataProvider()
    {
        TestAssert.True(typeof(IEndpointMetadataProvider)
            .IsAssignableFrom(typeof(ApiPilotSkipCsrfAttribute)));
        TestAssert.True(typeof(IEndpointMetadataProvider)
            .IsAssignableFrom(typeof(ApiPilotRequireCsrfAttribute)));
    }

    /// <summary>The two attributes are distinct types.</summary>
    [Test]
    public void Attributes_AreDistinctTypes()
    {
        var skip = new ApiPilotSkipCsrfAttribute();
        var require = new ApiPilotRequireCsrfAttribute();
        TestAssert.False(skip.GetType() == require.GetType());
    }
}

