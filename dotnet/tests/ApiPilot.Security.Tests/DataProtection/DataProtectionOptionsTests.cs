// filepath: dotnet/tests/ApiPilot.Security.Tests/DataProtection/DataProtectionOptionsTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Contract tests for the ApiPilotDataProtectionOptions and the SharedKeyConfiguration helper
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.Security.DataProtection,
//                Microsoft.AspNetCore.DataProtection, Microsoft.Extensions.DependencyInjection
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiPilotDataProtectionOptions.cs, SharedKeyConfiguration.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.DataProtection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPilot.Security.Tests.DataProtection;

/// <summary>
/// Contract tests for ApiPilotDataProtectionOptions and SharedKeyConfiguration.
/// Verifies the defaults and the KeyStorageConfigured flag lifecycle.
/// </summary>
[TestClass]
public sealed class DataProtectionOptionsTests
{
    /// <summary>All defaults are as documented.</summary>
    [Test]
    public void Defaults_AreAsDocumented()
    {
        var options = new ApiPilotDataProtectionOptions();
        TestAssert.Null(options.ApplicationName);
        TestAssert.Null(options.KeyLifetime);
        TestAssert.False(options.MultiInstance);
        TestAssert.Null(options.KeyStorage);
        TestAssert.False(options.KeyStorageConfigured);
    }

    /// <summary>ApplicationName is mutable.</summary>
    [Test]
    public void ApplicationName_IsMutable()
    {
        var options = new ApiPilotDataProtectionOptions { ApplicationName = "Test.App" };
        TestAssert.Equal("Test.App", options.ApplicationName);
    }

    /// <summary>KeyLifetime is mutable.</summary>
    [Test]
    public void KeyLifetime_IsMutable()
    {
        var options = new ApiPilotDataProtectionOptions { KeyLifetime = TimeSpan.FromDays(30) };
        TestAssert.Equal(TimeSpan.FromDays(30), options.KeyLifetime);
    }

    /// <summary>MultiInstance is mutable.</summary>
    [Test]
    public void MultiInstance_IsMutable()
    {
        var options = new ApiPilotDataProtectionOptions { MultiInstance = true };
        TestAssert.True(options.MultiInstance);
    }

    /// <summary>Apply does not set KeyStorageConfigured when the delegate is null.</summary>
    [Test]
    public void Apply_NullKeyStorage_DoesNotSetFlag()
    {
        var services = new ServiceCollection();
        var builder = services.AddDataProtection();
        var options = new ApiPilotDataProtectionOptions();

        SharedKeyConfiguration.Apply(builder, options);

        TestAssert.False(options.KeyStorageConfigured);
    }

    /// <summary>Apply sets KeyStorageConfigured when the delegate is present.</summary>
    [Test]
    public void Apply_WithKeyStorage_SetsFlag()
    {
        var services = new ServiceCollection();
        var builder = services.AddDataProtection();
        var invoked = false;
        var options = new ApiPilotDataProtectionOptions
        {
            KeyStorage = _ => { invoked = true; },
        };

        SharedKeyConfiguration.Apply(builder, options);

        TestAssert.True(invoked);
        TestAssert.True(options.KeyStorageConfigured);
    }
}

